import test from 'node:test';
import assert from 'node:assert/strict';
import {analyze} from '../analysis.mjs';
const request=content=>({protocolVersion:1,requestId:'request',snapshotId:'A'.repeat(64),content,locale:'en_US'});
test('real engine produces exactly three selected assessments and preserves poor score zero',()=>{
 const result=analyze(request('<h1>Care</h1><p>'+('We feed our puppies and help them settle at home. '.repeat(35))+'</p>'));
 assert.equal(result.engineVersion,'1.0.19');
 assert.deepEqual(result.assessments.map(x=>x.id),['subheading-distribution','transition-words','word-complexity']);
 assert.equal(result.assessments[0].status,'poor');assert.equal(result.assessments[0].score,0);
});
test('connected rewrite improves transitions without quotas or extra checks',()=>{
 const bad=analyze(request('<p>We feed puppies. We wash bedding. We check records. We meet families.</p>'));
 const good=analyze(request('<p>First, we feed puppies. Next, we wash bedding. Then, we check records. Finally, we meet families.</p>'));
 assert.equal(bad.assessments[1].status,'poor');assert.equal(good.assessments[1].status,'good');
 assert.equal(good.assessments[0].status,'na');
});
test('invalid payload, locale and excessive prepared HTML are rejected',()=>{
 for(const r of [request(null),{...request('text'),locale:'fr_FR'},{...request('text'),protocolVersion:99},request('x'.repeat(3000001))])
  assert.throws(()=>analyze(r));
});
