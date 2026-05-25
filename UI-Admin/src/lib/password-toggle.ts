// Show/hide-password eye on the login form.

export function initPasswordToggle(): void {
    const btn = document.getElementById('toggle-password');
    const input = document.getElementById('login-password') as HTMLInputElement | null;
    const eye = document.getElementById('pw-eye');
    const eyeOff = document.getElementById('pw-eye-off');
    if (!btn || !input || !eye || !eyeOff) return;

    btn.addEventListener('click', () => {
        const show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        eye.classList.toggle('hidden', show);
        eyeOff.classList.toggle('hidden', !show);
    });
}
