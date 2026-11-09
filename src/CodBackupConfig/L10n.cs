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
            ["sec.backups"]    = "BACKUPS",

            ["card.notfound"]  = "NOT FOUND ON THIS PC",
            ["card.account"]   = "{0} ACCOUNT",
            ["card.accounts"]  = "{0} ACCOUNTS",
            ["card.browse"]    = "choose folder…",

            ["legend.note"]    = "platform is a guess from the id shape — label only",

            ["btn.refresh"]    = "REFRESH",
            ["btn.backup"]     = "BACKUP ACCOUNT",
            ["btn.restore"]    = "RESTORE SNAPSHOT",
            ["btn.lockon"]     = "LOCK SETTINGS",
            ["btn.lockoff"]    = "UNLOCK SETTINGS",
            ["btn.players"]    = "OPEN PLAYERS FOLDER",
            ["btn.backups"]    = "OPEN BACKUPS FOLDER",

            ["lbl.name"]       = "BACKUP NAME",
            ["lbl.snapshot"]   = "SNAPSHOT",
            ["lbl.target"]     = "TARGET ACCOUNT ID",
            ["ph.name"]        = "my-backup",
            ["ph.none"]        = "(none yet)",
            ["ph.target"]      = "digits only",
            ["chk.replace"]    = "replace existing files",

            ["list.empty"]     = "(no accounts found — run the game once with each platform you use, then press refresh)",

            ["st.ready"]       = "",
            ["st.noacct"]      = "select an account first",
            ["st.nofolders"]   = "no players folder found — launch the game once, then press refresh",
            ["st.backupok"]    = "backup saved: {0}",
            ["st.restoreok"]   = "restored: {0} copied, {1} left untouched",
            ["st.made"]        = "folder created",
            ["st.lockon"]      = "locked {0} settings file(s) — read-only",
            ["st.lockoff"]     = "unlocked {0} settings file(s)",
            ["st.nosettings"]  = "no s.1.0.* settings files in this account",
            ["st.notarget"]    = "target account id must be digits (6+)",
            ["st.badname"]     = "backup name: letters, numbers, space, - and _ only (max 40)",
            ["st.nosnap"]      = "no snapshot selected",
            ["err"]            = "error: ",

            ["tip.game"]       = "Pick which Call of Duty to work with. The game keeps one folder per logged-in account inside its players directory; this app only copies files between those folders and your backups.",
            ["tip.browse"]     = "Auto-detection failed? Pick the players folder manually.",
            ["tip.refresh"]    = "Rescan the players folder for accounts and lock state.",
            ["tip.backup"]     = "Copy every file of the selected account into a dated snapshot under CodBackupConfigs\\backups. The original folder stays exactly as it is.",
            ["tip.restore"]    = "Copy a snapshot into the target account folder. Creates the folder if it does not exist yet. Files that already exist are kept unless 'replace existing' is ticked.",
            ["tip.lock"]       = "Mark the selected account's settings files (s.1.0.*) read-only so the game cannot reset them. Click again to unlock. Close the game first.",
            ["tip.players"]    = "Open the players folder of the selected game in Explorer.",
            ["tip.backups"]    = "Open the CodBackupConfigs backups folder in Explorer.",
            ["tip.name"]       = "Optional friendly name, shown in the snapshot list.",
            ["tip.snapshot"]   = "Pick a snapshot made earlier to restore it.",
            ["tip.target"]     = "Numeric id of the account to restore into. Must match a folder inside the players directory. Type a new id to stage settings for a fresh account.",
            ["tip.replace"]    = "Only for restoring: tick to overwrite files that already exist in the target account.",
            ["tip.list"]       = "One row per account the game remembers on this pc. Platform column is a best-effort guess from the id.",
            ["tip.en"]         = "Interface in English",
            ["tip.pt"]         = "Interface em Português",
        };

        static readonly Dictionary<string, string> Pt = new()
        {
            ["title"]          = "COD BACKUP CONFIGS",
            ["sec.game"]       = "JOGO",
            ["sec.accounts"]   = "CONTAS",
            ["sec.backups"]    = "BACKUPS",

            ["card.notfound"]  = "NÃO ENCONTRADO NESTE PC",
            ["card.account"]   = "{0} CONTA",
            ["card.accounts"]  = "{0} CONTAS",
            ["card.browse"]    = "escolher pasta…",

            ["legend.note"]    = "a plataforma é um chute pelo formato do id — só um rótulo",

            ["btn.refresh"]    = "ATUALIZAR",
            ["btn.backup"]     = "BACKUP DA CONTA",
            ["btn.restore"]    = "RESTAURAR SNAPSHOT",
            ["btn.lockon"]     = "TRAVAR CONFIGS",
            ["btn.lockoff"]    = "DESBLOQUEAR CONFIGS",
            ["btn.players"]    = "ABRIR PASTA DE PLAYERS",
            ["btn.backups"]    = "ABRIR PASTA DE BACKUPS",

            ["lbl.name"]       = "NOME DO BACKUP",
            ["lbl.snapshot"]   = "SNAPSHOT",
            ["lbl.target"]     = "ID DA CONTA DESTINO",
            ["ph.name"]        = "meu-backup",
            ["ph.none"]        = "(nenhum ainda)",
            ["ph.target"]      = "só números",
            ["chk.replace"]    = "substituir arquivos existentes",

            ["list.empty"]     = "(nenhuma conta encontrada — rode o jogo uma vez em cada plataforma que você usa e depois clique em atualizar)",

            ["st.ready"]       = "",
            ["st.noacct"]      = "selecione uma conta primeiro",
            ["st.nofolders"]   = "pasta de players não encontrada — abra o jogo uma vez e depois clique em atualizar",
            ["st.backupok"]    = "backup salvo: {0}",
            ["st.restoreok"]   = "restaurado: {0} copiados, {1} mantidos como estavam",
            ["st.made"]        = "pasta criada",
            ["st.lockon"]      = "{0} arquivo(s) de config travado(s) — somente leitura",
            ["st.lockoff"]     = "{0} arquivo(s) de config desbloqueado(s)",
            ["st.nosettings"]  = "nenhum arquivo s.1.0.* de config nesta conta",
            ["st.notarget"]    = "o id da conta de destino deve ser só números (6+)",
            ["st.badname"]     = "nome do backup: letras, números, espaço, - e _ apenas (máx. 40)",
            ["st.nosnap"]      = "nenhum snapshot selecionado",
            ["err"]            = "erro: ",

            ["tip.game"]       = "Escolha qual Call of Duty usar. O jogo guarda uma pasta por conta logada dentro do diretório de players; este app só copia arquivos entre essas pastas e seus backups.",
            ["tip.browse"]     = "A detecção automática falhou? Escolha a pasta de players manualmente.",
            ["tip.refresh"]    = "Escaneia a pasta de players de novo, procurando contas e estado de trava.",
            ["tip.backup"]     = "Copia todos os arquivos da conta selecionada para um snapshot com data em CodBackupConfigs\\backups. A pasta original continua exatamente como estava.",
            ["tip.restore"]    = "Copia um snapshot para a pasta da conta de destino. Cria a pasta se ela ainda não existir. Arquivos que já existem são mantidos, a menos que 'substituir arquivos existentes' esteja marcado.",
            ["tip.lock"]       = "Marca os arquivos de config da conta selecionada (s.1.0.*) como somente leitura para o jogo não conseguir resetá-los. Clique de novo para desbloquear. Feche o jogo antes.",
            ["tip.players"]    = "Abre a pasta de players do jogo selecionado no Explorer.",
            ["tip.backups"]    = "Abre a pasta de backups CodBackupConfigs no Explorer.",
            ["tip.name"]       = "Nome amigável opcional, aparece na lista de snapshots.",
            ["tip.snapshot"]   = "Escolha um snapshot feito antes para restaurar.",
            ["tip.target"]     = "Id numérico da conta que vai receber a restauração. Precisa casar com uma pasta dentro do diretório de players. Digite um id novo para deixar pronto para uma conta nova.",
            ["tip.replace"]    = "Só para restaurar: marque para sobrescrever arquivos que já existem na conta de destino.",
            ["tip.list"]       = "Uma linha por conta que o jogo lembra neste pc. A coluna de plataforma é um chute pelo formato do id.",
            ["tip.en"]         = "Interface em English",
            ["tip.pt"]         = "Interface em Português",
        };
    }
}
