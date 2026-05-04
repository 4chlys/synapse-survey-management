import './main.css';
import { initTheme } from './theme';
import { initSurvey } from './survey/init';
import { initChat } from './survey/chat';
import { initIdeation } from "./ideation/ideahub";

function init() {
    initTheme();
    initSurvey();
    initChat();
    initIdeation();

    requestAnimationFrame(() => {
        document.querySelectorAll<HTMLElement>('.page-enter').forEach(el => {
            el.classList.add('page-visible');
            setTimeout(() => el.classList.remove('page-enter', 'page-visible'), 450);
        });
    });
}

init();

