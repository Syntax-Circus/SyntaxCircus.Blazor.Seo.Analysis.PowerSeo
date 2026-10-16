import {checkSubheadingDistribution,checkTransitionWords,checkWordComplexity} from '@power-seo/content-analysis';
export const protocolVersion=1,engineVersion='1.0.19',maxContentCharacters=3000000;
export function validRequest(r){
 return r?.protocolVersion===protocolVersion&&typeof r.requestId==='string'&&r.requestId.length>0&&r.requestId.length<=64&&
  typeof r.snapshotId==='string'&&r.snapshotId.length>0&&r.snapshotId.length<=64&&typeof r.content==='string'&&r.content.length<=maxContentCharacters&&
  typeof r.locale==='string'&&/^en(?:[_-]|$)/i.test(r.locale);
}
export function analyze(request){
 if(!validRequest(request))throw new Error('Invalid analysis request');
 const input={content:request.content,locale:'en'};
 const assessments=[checkSubheadingDistribution(input),checkTransitionWords(input),checkWordComplexity(input)]
  .map(({id,status,score,description})=>({id,status,score,description}));
 return {protocolVersion,engineVersion,requestId:request.requestId,snapshotId:request.snapshotId,state:'Ready',assessments};
}
