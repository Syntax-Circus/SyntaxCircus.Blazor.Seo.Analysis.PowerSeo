import { chromium } from 'playwright';
import assert from 'node:assert/strict';
import { mkdir, writeFile, readFile } from 'node:fs/promises';

const base = process.env.SAMPLE_URL ?? 'http://127.0.0.1:5092/demo/';
const browser = await chromium.launch({ headless: true });
const evidence = [];
try {
  for (const mode of ['server', 'wasm']) {
    const context = await browser.newContext();
    if (process.env.CORPUS_INPUT)
      await context.route('**/fixtures.json', async route => route.fulfill({ contentType: 'application/json', body: await readFile(process.env.CORPUS_INPUT, 'utf8') }));
    const errors = [], violations = [], external = [];
    await context.addInitScript(() => {
      window.sampleWorkers = { started: 0, active: 0, peak: 0 };
      const NativeWorker = window.Worker;
      window.Worker = class extends NativeWorker {
        constructor(...args) {
          super(...args);
          const s = window.sampleWorkers;
          s.started++; s.active++; s.peak = Math.max(s.peak, s.active);
        }
        terminate() { if (!this.stopped) { this.stopped = true; window.sampleWorkers.active--; } return super.terminate(); }
      };
      window.sampleViolations = [];
      document.addEventListener('securitypolicyviolation', e => window.sampleViolations.push(e.violatedDirective));
    });
    const page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (new URL(request.url()).origin !== new URL(base).origin) external.push(request.url()); });
    await page.goto(new URL(mode, base).href);
    await page.locator('[data-editor="First"] button').waitFor();
    await page.waitForFunction(() => document.querySelector('[data-editor="First"] button')?.disabled === false);
    const first = page.locator('[data-editor="First"]');
    const second = page.locator('[data-editor="Second"]');
    await first.locator('button').click();
    await second.locator('button').click();
    await page.waitForFunction(() => [...document.querySelectorAll('[role=status]')].every(x => x.textContent === 'Checked'));
    assert.match(await first.locator('[data-coverage]').textContent(), /Complete/);
    assert.match(await second.locator('[data-coverage]').textContent(), /Complete/);
    // Rapid changes exercise debounce and latest-generation publication.
    for (const text of ['Old draft.', 'New draft.', '<p>First, we explain the plan. Next, we help you prepare. Finally, we answer your questions.</p>'])
      await first.locator('textarea').fill(text);
    await page.waitForTimeout(1000);
    await page.waitForFunction(() => document.querySelector('[data-editor="First"] [role=status]').textContent === 'Checked');
    await page.locator('#run-probe').click();
    await page.waitForFunction(() => document.querySelector('#probe-result').textContent.startsWith('['), null, { timeout: 60000 });
    const corpus = JSON.parse(await page.locator('#probe-result').textContent());
    assert.equal(corpus.length, process.env.CORPUS_INPUT ? JSON.parse(await readFile(process.env.CORPUS_INPUT, 'utf8')).length : 24);
    assert.ok(corpus.every(x => x.Preserved && (x.State === 'Complete' ? x.Raw === 3 && x.Decisions === 3 : x.Raw === 0)));
    assert.ok(corpus.some(x => x.State === 'Complete'));
    assert.ok(corpus.every(x => ['Complete', 'NotApplicable', 'UnsupportedLanguage'].includes(x.State)));
    const workers = await page.evaluate(() => window.sampleWorkers);
    assert.equal(workers.active, 0);
    assert.ok(workers.peak <= 2);
    violations.push(...await page.evaluate(() => window.sampleViolations));
    assert.deepEqual(violations, []);
    assert.deepEqual(errors, []);
    assert.deepEqual(external, []);
    evidence.push({ mode, corpus, workers, errors, violations, external });
    await context.close();
    const rewriting = await browser.newContext();
    await rewriting.addInitScript(() => {
      const Native = window.Worker;
      window.workerCounts = { started: 0, active: 0 };
      window.Worker = class extends Native {
        constructor(...args) { super(...args); window.workerCounts.started++; window.workerCounts.active++; }
        terminate() { if (!this.stopped) { this.stopped = true; window.workerCounts.active--; } super.terminate(); }
      };
    });
    await rewriting.route('**/power-seo.worker.js', async route => {
      const response = await route.fetch();
      await route.fulfill({ response, body: 'await new Promise(r => setTimeout(r, 1200));\n' + await response.text() });
    });
    const editPage = await rewriting.newPage();
    await editPage.goto(new URL(mode, base).href);
    await editPage.waitForFunction(() => document.querySelector('[data-editor="First"] button')?.disabled === false);
    const edit = editPage.locator('[data-editor="First"]');
    await edit.locator('button').click();
    await editPage.waitForFunction(() => window.workerCounts.started === 1);
    const latest = '<p>First, we explain the latest plan. Next, we help you prepare.</p>';
    await edit.locator('textarea').fill(latest);
    await edit.locator('button').click();
    await editPage.waitForFunction(() => document.querySelector('[data-editor="First"] [role=status]').textContent === 'Checked');
    assert.equal(await edit.locator('[data-checked-text]').textContent(), latest);
    assert.deepEqual(await editPage.evaluate(() => window.workerCounts), { started: 2, active: 0 });
    evidence.push({ mode, scenario: 'active-cancellation-and-rewrite', latestPublished: true, workersTerminated: true });
    await rewriting.close();
    for (const [scenario, state, workerCode] of [
      ['missing', 'Unavailable', null],
      ['error', 'Failed', "throw new Error('qualification failure')"],
      ['malformed', 'Failed', "self.onmessage = () => self.postMessage({ invalid: true })"],
      ['stale', 'Stale', "self.onmessage = ({data:r}) => self.postMessage({protocolVersion:1,requestId:'old',snapshotId:r.snapshotId,engineVersion:'1.0.19',state:'Ready',assessments:[]})"],
      ['timeout', 'TimedOut', "self.onmessage = () => {}"]
    ]) {
      const degraded = await browser.newContext();
      if (scenario === 'missing') await degraded.addInitScript(() => { window.Worker = undefined; });
      else await degraded.route('**/power-seo.worker.js', route => route.fulfill({ contentType: 'text/javascript', body: workerCode }));
      const probe = await degraded.newPage();
      await probe.goto(new URL(mode, base).href);
      const editor = probe.locator('[data-editor="First"]');
      await probe.waitForFunction(() => document.querySelector('[data-editor="First"] button')?.disabled === false);
      await editor.locator('button').click();
      await probe.waitForFunction(() => document.querySelector('[data-editor="First"] [role=status]').textContent === 'Checked');
      assert.match(await editor.locator('[data-coverage]').textContent(), new RegExp(state));
      assert.match(await editor.textContent(), /Add a search title/);
      evidence.push({ mode, scenario, state, baselinePreserved: true });
      await degraded.close();
    }
  }
  await mkdir('artifacts', { recursive: true });
  await writeFile('artifacts/browser' + (process.env.EVIDENCE_SUFFIX ?? '') + '.json', JSON.stringify(evidence, null, 2));
  console.log(JSON.stringify(evidence.map(({ mode, corpus, workers, scenario, state }) => ({ mode, fixtures: corpus?.length, workers, scenario, state }))));
} finally { await browser.close(); }
