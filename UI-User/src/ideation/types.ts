export interface EmojiReaction {
    emojiId: number;
    symbol: string;
    count: number;
    postedByCurrentUser: boolean;
}

export interface IdeaResponse {
    ideaId: number;
    description: string;
    commentsCount: number;
    isOwnedByCurrentUser: boolean;
    isFlagged: boolean;
    languageCode?: string;
    emojis: EmojiReaction[];
}

export interface LoadIdeasResponse {
    ideas: IdeaResponse[];
    remaining: number;
}