import test,{beforeEach} from 'node:test';
import assert from 'node:assert/strict';
import {createSession} from '../bridge.mjs';
import {analyze} from '../analysis.mjs';
const instances=[];
class FakeWorker {
 listeners={};terminated=false;
 constructor(){instances.push(this);}
 addEventListener(type,callback){this.listeners[type]=callback;}
 postMessage(request){this.request=request;}
 terminate(){this.terminated=true;}
 complete(){this.listeners.message({data:analyze(this.request)});}
}
beforeEach(()=>{instances.length=0;globalThis.Worker=FakeWorker;});
const request=id=>({protocolVersion:1,requestId:id,snapshotId:'A'.repeat(64),content:'<p>We feed puppies.</p>',locale:'en_US'});
test('two independent sessions cannot cancel one another',async()=>{
 const a=createSession(),b=createSession();
 const pa=a.analyze(request('same'),1000),pb=b.analyze(request('same'),1000);
 a.cancel('same');assert.equal((await pa).state,'Failed');
 assert.equal(instances[0].terminated,true);assert.equal(instances[1].terminated,false);
 instances[1].complete();assert.equal((await pb).state,'Ready');assert.equal(instances[1].terminated,true);
 a.dispose();b.dispose();
});
test('capacity, timeout and disposal terminate all workers without a queue',async()=>{
 const session=createSession();
 const a=session.analyze(request('a'),20),b=session.analyze(request('b'),1000);
 assert.equal((await session.analyze(request('c'),1000)).state,'Unavailable');
 assert.equal((await a).state,'TimedOut');
 session.dispose();assert.equal((await b).state,'Unavailable');
 assert.ok(instances.every(x=>x.terminated));
 assert.equal((await session.analyze(request('d'),1000)).state,'Unavailable');
});
test('worker errors and malformed or oversize results fail closed',async()=>{
 for(const mode of ['error','bad','long']){
  const session=createSession(),pending=session.analyze(request(mode),1000),worker=instances.at(-1);
  if(mode==='error')worker.listeners.error({preventDefault(){}});
  else {const response=analyze(worker.request);response.assessments[0].description=mode==='long'?'x'.repeat(2001):null;worker.listeners.message({data:response});}
  assert.equal((await pending).state,'Failed');assert.equal(worker.terminated,true);session.dispose();
 }
});
test('missing Worker API is explicitly unavailable',async()=>{
 delete globalThis.Worker;
 const session=createSession();assert.equal((await session.analyze(request('a'),1000)).state,'Unavailable');session.dispose();
});
test('unsupported replies carry bounded fields only',async()=>{
 const session=createSession(),pending=session.analyze(request('a'),1000),worker=instances.at(-1);
 worker.listeners.message({data:{...analyze(worker.request),protocolVersion:2,extra:'x'.repeat(100000)}});
 const result=await pending;
 assert.equal(result.protocolVersion,2);assert.equal(result.extra,undefined);assert.deepEqual(result.assessments,[]);
 session.dispose();
});
