import './main.css';
import { initTheme } from './theme';
import { initDomHandlers } from './lib/dom-handlers';
import { initAlerts } from './lib/alerts';
import { initPasswordToggle } from './lib/password-toggle';
import { initExportEmails } from './lib/export-emails';
import { initSettingsPrompts } from './lib/settings-prompts';
import { initSettingsRadio } from './lib/settings-radio';
import { initProjectIndex } from './lib/project-index';
import { initProjectSetup } from './project/project-setup';
import { initProjectStyle } from './lib/project-style';
import { initShortformAutofill } from './lib/project-shortform-autofill';
import { initProjectQuestions } from './lib/project-questions';
import { init as initQuestionDetailModal } from './project/question-detail-modal';
import { initQuestionnaireDnd } from './lib/questionnaire-dnd';
import { initSupportChatPage } from './lib/questionsupportchat';
import { initProjectIdeation } from './lib/project-ideation';
import { initIdeasFilter } from './lib/ideas-filter';
import { initIdeasStatusBadges } from './lib/ideas-status-badge';
import { initEmailCompose } from './lib/email-compose';
import { initEmailBroadcast } from './lib/email-broadcast';
import { initRawdataExport } from './lib/rawdata-export';
import { initSystemStatsExport } from './lib/system-stats-export';
import { initProjectStatsExport } from './lib/project-stats-export';
import { initConfigureDashboard } from './lib/configure-dashboard';
import { initDashboard } from './admin/dashboard';

function init(): void {
    initTheme();
    initDomHandlers();
    initAlerts();
    initPasswordToggle();
    initExportEmails();
    initSettingsPrompts();
    initSettingsRadio();
    initProjectIndex();
    initProjectSetup();
    initProjectStyle();
    initShortformAutofill();
    initProjectQuestions();
    initQuestionDetailModal();
    void initQuestionnaireDnd();
    initSupportChatPage();
    initProjectIdeation();
    initIdeasFilter();
    initIdeasStatusBadges();
    initEmailCompose();
    initEmailBroadcast();
    initRawdataExport();
    initSystemStatsExport();
    initProjectStatsExport();
    initConfigureDashboard();
    initDashboard();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}
