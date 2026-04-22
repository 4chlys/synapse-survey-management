import './main.css';
import { initTheme } from './theme';
import { initSurvey } from './survey/init';
import { initIdeation } from "./ideation/ideahub";

initTheme();
initSurvey();
initIdeation();

// Trigger page entrance — rAF ensures the browser has painted the
// initial (opacity:0, translateY:20px) state before the transition fires.
// Classes are removed 50 ms after the transition ends so the element has
// no transform and no stacking context — fixed nav stays fully clickable.
requestAnimationFrame(() => {
    document.querySelectorAll<HTMLElement>('.page-enter').forEach(el => {
        el.classList.add('page-visible');
        setTimeout(() => el.classList.remove('page-enter', 'page-visible'), 450);
    });
});