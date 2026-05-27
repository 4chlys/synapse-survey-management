import type { QuestionTranslation, EndingAnswerSlot } from '../lib/questionnaire-tree';

export interface MovePayload {
    parentQuestionId: number | null;
    choiceIndex: number | null;
    position: number;
}

export interface EndingPayload {
    endingAnswers: string[];
}

export interface AdminQuestionResponse {
    questionId: number;
    questionFormat: string;
    isMandatory: boolean;
    position: number;
    answerChoiceCount: number;
    choiceIndex: number | null;
    parentQuestionId: number | null;
    sectionId: number | null;
    sectionName: string | null;
    mediaUrl: string | null;
    mediaType: number | null;
    mediaIsEmbed: boolean;
    translations: QuestionTranslation[];
    endingAnswers: EndingAnswerSlot[];
}
