import { initStatWidget } from '../widgets/widget-stat';
import { initTimelineWidget } from '../widgets/widget-timeline';
import { initTopCategoriesWidget } from '../widgets/widget-top-categories';
import { initTopIdeasWidget } from '../widgets/widget-top-ideas';
import { initSurveyDistributionWidget } from '../lib/widget-survey-distribution';
import { initIdeaFeedWidget } from '../widgets/widget-idea-feed';
import { initModerationQueueWidget } from '../widgets/widget-moderation-queue';
import { initSystemParticipantsWidget } from '../widgets/widget-system-participants';
import { initSystemProjectStatesWidget } from '../widgets/widget-system-project-states';
import { initSystemAiCostWidget } from '../widgets/widget-system-ai-cost';
import { initSystemCompletionBoxplotWidget } from '../widgets/widget-system-completion-boxplot';
import { initQuestionSinglePieWidget } from '../widgets/widget-question-single-pie';
import { initQuestionMultiBarWidget } from '../widgets/widget-question-multi-bar';
import { initQuestionLikertBarWidget } from '../widgets/widget-question-likert-bar';

const POLL_INTERVAL_MS = 30_000;

type WidgetInit = (el: HTMLElement) => (() => void) | void;

const widgetRegistry: Record<string, WidgetInit> = {
    ViewCount: initStatWidget,
    CompletionCount: initStatWidget,
    CompletionRate: initStatWidget,
    ParticipationTimeline: initTimelineWidget,
    TopCategories: initTopCategoriesWidget,
    TopIdeas: initTopIdeasWidget,
    SurveyDistribution: initSurveyDistributionWidget,
    IdeaFeed: initIdeaFeedWidget,
    ModerationQueue: initModerationQueueWidget,
    SystemParticipants: initSystemParticipantsWidget,
    SystemProjectStates: initSystemProjectStatesWidget,
    SystemAiCost: initSystemAiCostWidget,
    SystemCompletionBoxplot: initSystemCompletionBoxplotWidget,
    QuestionSinglePie: initQuestionSinglePieWidget,
    QuestionMultiBar: initQuestionMultiBarWidget,
    QuestionLikertBar: initQuestionLikertBarWidget,
};

document.addEventListener('DOMContentLoaded', () => {
    const slots = document.querySelectorAll<HTMLElement>('[data-widget]');
    const cleanups: Array<() => void> = [];

    slots.forEach(el => {
        if (el.dataset.widgetInitialized === 'true') return;
        el.dataset.widgetInitialized = 'true';

        const type = el.dataset.widget ?? '';
        const init = widgetRegistry[type];
        if (!init) return;

        const cleanup = init(el);
        if (cleanup) cleanups.push(cleanup);
    });

    window.addEventListener('beforeunload', () => cleanups.forEach(fn => fn()));
});

export { POLL_INTERVAL_MS };

const inflightRequests = new Map<string, Promise<unknown>>();

export function fetchJson<T>(url: string): Promise<T> {
    if (!inflightRequests.has(url)) {
        const p = fetch(url)
            .then(r => { if (!r.ok) throw new Error(`HTTP ${r.status}`); return r.json() as Promise<T>; })
            .finally(() => inflightRequests.delete(url));
        inflightRequests.set(url, p);
    }
    return inflightRequests.get(url) as Promise<T>;
}
