import {analyze} from './analysis.mjs';
self.addEventListener('message',event=>self.postMessage(analyze(event.data)));
