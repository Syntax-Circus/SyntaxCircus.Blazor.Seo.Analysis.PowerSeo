import { spawn } from 'node:child_process';
import { mkdir, open } from 'node:fs/promises';
await mkdir('artifacts', { recursive: true });
for (const prefix of ['', '/demo']) {
  const log = await open('artifacts/sample' + (prefix ? '-subpath' : '') + '.log', 'w');
  const host = spawn('dotnet', ['bin/Release/net10.0/BlazorHost.dll',
    '--urls', 'http://127.0.0.1:5092', '--environment', 'Development', '--SamplePathBase', prefix || '/',
    '--Logging:LogLevel:Default', 'Warning'], { cwd: 'samples/BlazorHost', stdio: ['ignore', log.fd, log.fd] });
  try {
    let ready = false;
    for (let attempt = 0; attempt < 100; attempt++) {
      try { if ((await fetch('http://127.0.0.1:5092' + prefix + '/server')).ok) { ready = true; break; } } catch {}
      if (host.exitCode !== null) throw new Error('Host exited: ' + host.exitCode);
      await new Promise(resolve => setTimeout(resolve, 200));
    }
    if (!ready) throw new Error('Host did not become ready');
    const test = spawn(process.execPath, ['scripts/browser.mjs'], { stdio: 'inherit',
      env: { ...process.env, SAMPLE_URL: 'http://127.0.0.1:5092' + prefix + '/', EVIDENCE_SUFFIX: prefix ? '-subpath' : '' } });
    const code = await new Promise(resolve => test.once('exit', resolve));
    if (code !== 0) throw new Error('Browser tests exited: ' + code);
  } finally {
    host.kill();
    await new Promise(resolve => host.exitCode !== null ? resolve() : host.once('exit', resolve));
    await log.close();
  }
}
