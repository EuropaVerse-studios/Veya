import * as path from 'path';
import { workspace, ExtensionContext } from 'vscode';
import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions
} from 'vscode-languageclient/node';

let client: LanguageClient;

export function activate(context: ExtensionContext) {
    // Path al backend C# compilato di Veya LSP
    // Nota per lo sviluppo: assumiamo che Veya.LSP sia già buildato in VeyaSystem/Veya.LSP/bin/Debug/net9.0/Veya.LSP.exe
    const serverPath = context.asAbsolutePath(
        path.join('..', '..', 'VeyaSystem', 'Veya.LSP', 'bin', 'Debug', 'net9.0', 'Veya.LSP.exe')
    );

    const serverOptions: ServerOptions = {
        run: { command: serverPath },
        debug: { command: serverPath }
    };

    const clientOptions: LanguageClientOptions = {
        documentSelector: [{ scheme: 'file', language: 'veya' }],
        synchronize: {
            fileEvents: workspace.createFileSystemWatcher('**/.clientrc')
        }
    };

    client = new LanguageClient(
        'veyaLanguageServer',
        'Veya Language Server',
        serverOptions,
        clientOptions
    );

    client.start();
}

export function deactivate(): Thenable<void> | undefined {
    if (!client) {
        return undefined;
    }
    return client.stop();
}
