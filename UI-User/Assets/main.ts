import './main.css';
import { setApiBase } from './ideation/moderation';
import { initSurvey } from './survey/init';
import { initIdeation } from "./ideation/ideahub";

const apiBase = document.querySelector<HTMLMetaElement>('meta[name="api-base"]')?.content ?? '';
if (!apiBase) {
    console.warn('[moderation] No <meta name="api-base"> found. Moderation API calls will fail.');
}
setApiBase(apiBase);

initSurvey();
initIdeation();