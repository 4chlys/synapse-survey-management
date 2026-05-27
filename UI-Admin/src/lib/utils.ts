export function getAntiForgeryToken(): string {
    return document.querySelector<HTMLInputElement>('input[name="__RequestVerificationToken"]')?.value ?? '';
}
