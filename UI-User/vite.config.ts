import { UserConfig, defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite';
import fs from 'fs';
import path from 'path';

export default defineConfig(async () => {
    const files = fs.readdirSync('./Assets');
    const inputEntries = files
        .filter(file => file.endsWith('.ts'))
        .reduce((acc, file) => {
            const fileName = path.parse(file).name;
            acc[fileName] = path.join('./Assets', file);
            return acc;
        }, {} as Record<string, string>);

    const config: UserConfig = {
        plugins: [tailwindcss()],
        appType: 'custom',
        root: 'Assets',
        publicDir: 'public',
        build: {
            emptyOutDir: true,
            manifest: true,
            outDir: '../wwwroot',
            assetsDir: '',
            rollupOptions: {
                input: inputEntries
            },
        },
        server: {
            strictPort: true,
        },
        optimizeDeps: {
            include: [],
            exclude: ['@tailwindcss/vite']
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
