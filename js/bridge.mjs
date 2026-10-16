// Each .NET companion owns a distinct session, even when the ES module is shared.
const protocolVersion=1,engineVersion='1.0.19',capacity=2,maxContentCharacters=3000000,maxMessageCharacters=2000;
export function createSession(){
 const active=new Map();let disposed=false;
 const empty=(r,state)=>({protocolVersion,engineVersion,requestId:r?.requestId??'',snapshotId:r?.snapshotId??'',state,assessments:[]});
 return {
  analyze(request,timeout){
   if(disposed||typeof Worker==='undefined'||active.size>=capacity)return Promise.resolve(empty(request,'Unavailable'));
   if(!request||request.protocolVersion!==protocolVersion||typeof request.requestId!=='string'||!request.requestId||request.requestId.length>64||
      typeof request.snapshotId!=='string'||!request.snapshotId||request.snapshotId.length>64||typeof request.content!=='string'||request.content.length>maxContentCharacters||
      typeof request.locale!=='string'||!/^en(?:[_-]|$)/i.test(request.locale)||!Number.isFinite(timeout)||timeout<=0||timeout>60000||active.has(request.requestId))
     return Promise.resolve(empty(request,'Failed'));
   return new Promise(resolve=>{
    let worker,timer,finished=false;
    const finish=result=>{
     if(finished)return;finished=true;clearTimeout(timer);active.delete(request.requestId);
     worker?.terminate();resolve(result);
    };
    active.set(request.requestId,{finish:state=>finish(empty(request,state))});
    timer=setTimeout(()=>finish(empty(request,'TimedOut')),timeout);
    try {
     worker=new Worker(new URL('./power-seo.worker.js',import.meta.url),{type:'module',name:'syntaxcircus-power-seo'});
     worker.addEventListener('error',e=>{e.preventDefault?.();finish(empty(request,'Failed'));});
     worker.addEventListener('messageerror',()=>finish(empty(request,'Failed')));
     worker.addEventListener('message',event=>{
      const r=event.data;
      if(!r||typeof r.requestId!=='string'||r.requestId.length>64||typeof r.snapshotId!=='string'||r.snapshotId.length>64||
         typeof r.engineVersion!=='string'||r.engineVersion.length>32||!Number.isInteger(r.protocolVersion))
       return finish(empty(request,'Failed'));
      // Preserve bounded mismatches so C# can distinguish stale/unsupported output.
      if(r.requestId!==request.requestId||r.snapshotId!==request.snapshotId||r.protocolVersion!==protocolVersion||r.engineVersion!==engineVersion)
       return finish({protocolVersion:r.protocolVersion,engineVersion:r.engineVersion,requestId:r.requestId,snapshotId:r.snapshotId,state:'Failed',assessments:[]});
      if(r.state!=='Ready'||!Array.isArray(r.assessments)||r.assessments.length!==3||
        r.assessments.some(x=>!x||typeof x.id!=='string'||!x.id||x.id.length>64||!['good','na','ok','poor'].includes(x.status)||
          !Number.isFinite(x.score)||typeof x.description!=='string'||x.description.length>maxMessageCharacters))
       return finish(empty(request,'Failed'));
      // Do not return arbitrary worker properties or unbounded payloads through SignalR.
      finish({protocolVersion,engineVersion,requestId:r.requestId,snapshotId:r.snapshotId,state:'Ready',
       assessments:r.assessments.map(({id,status,score,description})=>({id,status,score,description}))});
     });
     worker.postMessage(request);
    }catch{finish(empty(request,'Unavailable'));}
   });
  },
  cancel(id){active.get(id)?.finish('Failed');},
  dispose(){disposed=true;for(const entry of [...active.values()])entry.finish('Unavailable');}
 };
}
