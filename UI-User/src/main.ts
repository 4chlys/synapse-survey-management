import './main.css';
import './lib/i18n';
import { initTheme } from './lib/theme';
import { initScrollHeader } from './lib/scroll-header';
import { initSurvey } from './survey/init';
import { initChat } from './survey/chat';
import { initIdeation } from "./ideation/ideahub";
import { initThankYouPopup } from './lib/thank-you-popup';
import { initSpeechToTextButtons } from './lib/speechtotext-attach';

function init() {
    initTheme();
    initScrollHeader();
    initSurvey();
    initChat();
    initIdeation();
    initThankYouPopup();
    initSpeechToTextButtons();

    requestAnimationFrame(() => {
        document.querySelectorAll<HTMLElement>('.page-enter').forEach(el => {
            el.classList.add('page-visible');
            setTimeout(() => el.classList.remove('page-enter', 'page-visible'), 450);
        });
    });
}

init();

