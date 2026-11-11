using System.Collections.Generic;

namespace CodBackupConfig
{
    // every user-facing string lives here. nothing hardcoded in form code.
    static class L10n
    {
        public static string Lang = "en";

        static Dictionary<string, string> Dict => Lang == "pt" ? Pt : En;

        public static string T(string key) =>
            Dict.TryGetValue(key, out var v) ? v : key;

        static readonly Dictionary<string, string> En = new()
        {
            ["title"]          = "COD BACKUP CONFIGS",
            ["sec.game"]       = "GAME",
            ["sec.accounts"]   = "ACCOUNTS",
            ["sec.backups"]    = "PRESETS",

            ["card.notfound"]  = "NOT FOUND ON THIS PC",
            ["card.account"]   = "{0} ACCOUNT",
            ["card.accounts"]  = "{0} ACCOUNTS",
            ["card.browse"]    = "choose folder…",

            ["btn.refresh"]    = "REFRESH",
            ["btn.backup"]     = "SAVE PRESET",
            ["btn.restore"]    = "APPLY PRESET",
            ["btn.lockon"]     = "LOCK CONFIGS",
            ["btn.lockoff"]    = "UNLOCK CONFIGS",
            ["btn.players"]    = "OPEN PLAYERS FOLDER",
            ["btn.backups"]    = "OPEN BACKUPS FOLDER",

            ["sec.save.tag"]     = "SAVE PRESET",
            ["sec.save.hint"]    = "will save {0} · {1} ({2} files)",
            ["sec.save.noselect"] = "select an account in the list above",
            ["sec.save.note"]    = "copy only — the original folder stays untouched",
            ["sec.restore.tag"]  = "APPLY PRESET",
            ["sec.restore.hint"] = "pick a preset, then pick the account that receives it",
            ["ph.name"]          = "name (optional)",
            ["ph.none"]        = "(none yet)",
            ["lbl.to"]         = "TO",
            ["chk.replace"]    = "replace existing files",

            ["list.empty"]     = "(no accounts found — run the game once with each platform you use, then press refresh)",
            ["list.files"]     = "{0} FILES",
            ["list.locked"]    = "{0} LOCKED",

            ["st.ready"]       = "",
            ["st.noacct"]      = "select an account first",
            ["st.nofolders"]   = "no players folder found — launch the game once, then press refresh",
            ["st.backupok"]    = "preset saved: {0}",
            ["st.restoreok"]   = "preset applied: {0} copied, {1} left untouched",
            ["st.made"]        = "folder created",
            ["st.lockon"]      = "locked {0} settings file(s) — read-only",
            ["st.lockoff"]     = "unlocked {0} settings file(s)",
            ["st.nosettings"]  = "no settings files found in this account",
            ["st.notarget"]    = "pick the account that receives the preset",
            ["st.badname"]     = "preset name: letters, numbers, space, - and _ only (max 40)",
            ["st.nosnap"]      = "no preset selected",
            ["err"]            = "error: ",

            ["tip.game"]       = "Pick which Call of Duty to work with. The game keeps one folder per logged-in account inside its players directory; this app only copies files between those folders and your backups.",
            ["tip.browse"]     = "Auto-detection failed? Pick the players folder manually.",
            ["tip.refresh"]    = "Rescan the players folder for accounts and lock state.",
            ["tip.backup"]     = "Save the selected account's configs as a preset under CodBackupConfigs\\backups, dated automatically. The original folder stays exactly as it is.",
            ["tip.restore"]    = "Apply the picked preset onto the chosen account. Creates the account folder if it does not exist yet. Files that already exist are kept unless 'replace existing' is ticked.",
            ["tip.lock"]       = "Mark every settings file of the selected account (all files except .swp swap files) read-only so the game cannot reset them. Click again to unlock. Close the game first.",
            ["tip.players"]    = "Open the players folder of the selected game in Explorer.",
            ["tip.backups"]    = "Open the CodBackupConfigs backups folder in Explorer.",
            ["tip.name"]       = "Optional name, shown in the preset list.",
            ["tip.snapshot"]   = "Pick a saved preset to apply.",
            ["tip.target"]     = "The account that will receive the preset. Follows your selection in the list; pick another to copy across accounts.",
            ["tip.replace"]    = "Only for restoring: tick to overwrite files that already exist in the target account.",
            ["list.tip"]       = "One row per account the game remembers on this pc. FILES = everything in the account folder; LOCKED = settings files kept read-only (all except .swp swap files).",
            ["tip.en"]         = "Interface in English",
            ["tip.pt"]         = "Interface em Português",
            ["tip.min"]        = "Minimize window",
            ["tip.max"]        = "Maximize / restore window",
            ["tip.close"]      = "Close Cod Backup Configs",
        };

        static readonly Dictionary<string, string> Pt = new()
        {
            ["title"]          = "COD BACKUP CONFIGS",
            ["sec.game"]       = "JOGO",
            ["sec.accounts"]   = "CONTAS",
            ["sec.backups"]    = "PRESETS",

            ["card.notfound"]  = "NÃO ENCONTRADO NESTE PC",
            ["card.account"]   = "{0} CONTA",
            ["card.accounts"]  = "{0} CONTAS",
            ["card.browse"]    = "escolher pasta…",

            ["btn.refresh"]    = "ATUALIZAR",
            ["btn.backup"]     = "SALVAR PRESET",
            ["btn.restore"]    = "APLICAR PRESET",
            ["btn.lockon"]     = "TRAVAR CONFIGS",
            ["btn.lockoff"]    = "DESBLOQUEAR CONFIGS",
            ["btn.players"]    = "ABRIR PASTA DE PLAYERS",
            ["btn.backups"]    = "ABRIR PASTA DE BACKUPS",

            ["sec.save.tag"]     = "SALVAR PRESET",
            ["sec.save.hint"]    = "vai salvar {0} · {1} ({2} arquivos)",
            ["sec.save.noselect"] = "selecione uma conta na lista acima",
            ["sec.save.note"]    = "só copia — a pasta original fica intacta",
            ["sec.restore.tag"]  = "APLICAR PRESET",
            ["sec.restore.hint"] = "escolha um preset e depois a conta que recebe",
            ["ph.name"]          = "nome (opcional)",
            ["ph.none"]        = "(nenhum ainda)",
            ["lbl.to"]         = "PARA",
            ["chk.replace"]    = "substituir arquivos existentes",

            ["list.empty"]     = "(nenhuma conta encontrada — rode o jogo uma vez em cada plataforma que você usa e depois clique em atualizar)",
            ["list.files"]     = "{0} ARQUIVOS",
            ["list.locked"]    = "{0} TRAVADOS",

            ["st.ready"]       = "",
            ["st.noacct"]      = "selecione uma conta primeiro",
            ["st.nofolders"]   = "pasta de players não encontrada — abra o jogo uma vez e depois clique em atualizar",
            ["st.backupok"]    = "preset salvo: {0}",
            ["st.restoreok"]   = "preset aplicado: {0} copiados, {1} mantidos como estavam",
            ["st.made"]        = "pasta criada",
            ["st.lockon"]      = "{0} arquivo(s) de config travado(s) — somente leitura",
            ["st.lockoff"]     = "{0} arquivo(s) de config desbloqueado(s)",
            ["st.nosettings"]  = "nenhum arquivo de config encontrado nesta conta",
            ["st.notarget"]    = "escolha a conta que recebe o preset",
            ["st.badname"]     = "nome do preset: letras, números, espaço, - e _ apenas (máx. 40)",
            ["st.nosnap"]      = "nenhum preset selecionado",
            ["err"]            = "erro: ",

            ["tip.game"]       = "Escolha qual Call of Duty usar. O jogo guarda uma pasta por conta logada dentro do diretório de players; este app só copia arquivos entre essas pastas e seus backups.",
            ["tip.browse"]     = "A detecção automática falhou? Escolha a pasta de players manualmente.",
            ["tip.refresh"]    = "Escaneia a pasta de players de novo, procurando contas e estado de trava.",
            ["tip.backup"]     = "Salva os configs da conta selecionada como preset com data em CodBackupConfigs\\backups. A pasta original continua exatamente como estava.",
            ["tip.restore"]    = "Aplica o preset escolhido na conta escolhida. Cria a pasta da conta se ela ainda não existir. Arquivos que já existem são mantidos, a menos que 'substituir arquivos existentes' esteja marcado.",
            ["tip.lock"]       = "Marca todos os arquivos de config da conta selecionada (tudo exceto arquivos .swp) como somente leitura para o jogo não conseguir resetá-los. Clique de novo para desbloquear. Feche o jogo antes.",
            ["tip.players"]    = "Abre a pasta de players do jogo selecionado no Explorer.",
            ["tip.backups"]    = "Abre a pasta de backups CodBackupConfigs no Explorer.",
            ["tip.name"]       = "Nome opcional, aparece na lista de presets.",
            ["tip.snapshot"]   = "Escolha um preset salvo para aplicar.",
            ["tip.target"]     = "A conta que vai receber o preset. Segue sua seleção na lista; escolha outra para copiar entre contas.",
            ["tip.replace"]    = "Só para restaurar: marque para sobrescrever arquivos que já existem na conta de destino.",
            ["list.tip"]       = "Uma linha por conta que o jogo lembra neste pc. ARQUIVOS = tudo na pasta da conta; TRAVADOS = configs em somente leitura (tudo exceto .swp).",
            ["tip.en"]         = "Interface em English",
            ["tip.pt"]         = "Interface em Português",
            ["tip.min"]        = "Minimizar janela",
            ["tip.max"]        = "Maximizar / restaurar janela",
            ["tip.close"]      = "Fechar Cod Backup Configs",
        };
    }
}
