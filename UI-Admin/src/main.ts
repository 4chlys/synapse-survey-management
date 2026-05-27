import './main.css';
import { initTheme } from './theme';
import { initDomHandlers } from './lib/dom-handlers';
import { initAlerts } from './lib/alerts';
import { initPasswordToggle } from './lib/password-toggle';
import { initExportEmails } from './lib/export-emails';
import { initSettingsPrompts } from './lib/settings-prompts';
import { initSettingsRadio } from './lib/settings-radio';
import { initProjectIndex } from './project/project-index';
import { initProjectSetup } from './project/project-setup';
import { initProjectStyle } from './project/project-style';
import { initShortformAutofill } from './project/project-shortform-autofill';
import { initProjectQuestions } from './project/project-questions';
import { init as initQuestionDetailModal } from './project/question-detail-modal';
import { initQuestionnaireDnd } from './questionnaire/questionnaire-dnd';
import { initSupportChatPage } from './questionnaire/questionsupportchat';
import { initProjectIdeation } from './project/project-ideation';
import { initIdeasFilter } from './admin/ideas-filter';
import { initIdeasStatusBadges } from './admin/ideas-status-badge';
import { initEmailCompose } from './admin/email-compose';
import { initEmailBroadcast } from './admin/email-broadcast';
import { initRawdataExport } from './admin/rawdata-export';
import { initSystemStatsExport } from './admin/system-stats-export';
import { initProjectStatsExport } from './project/project-stats-export';
import { initConfigureDashboard } from './project/configure-dashboard';
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
