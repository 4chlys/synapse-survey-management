import { UserConfig, defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import { readdirSync } from 'fs';
import { parse, join } from 'path';

export default defineConfig(async () => {
    const files = readdirSync('./src');
    const inputEntries = files
        .filter((file: string) => file.endsWith('.ts'))
        .reduce((acc: Record<string, string>, file: string) => {
            const fileName = parse(file).name;
            acc[fileName] = join('./src', file);
            return acc;
        }, {} as Record<string, string>);

    const config: UserConfig = {
        plugins: [tailwindcss()],
        appType: 'custom',
        base: '/dist/',
        root: 'src',
        publicDir: 'public',
        build: {
            emptyOutDir: false,
            manifest: true,
            outDir: '../wwwroot/dist',
            assetsDir: '',
            rollupOptions: {
                input: inputEntries
            },
        },
        server: {
            strictPort: true,
        },
        css: {
            preprocessorOptions: {
                scss: {
                    api: "modern-compiler",
                },
            },
        },
    }

    return config;
});
