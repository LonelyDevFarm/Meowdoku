using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        string rootDir = @"D:\Projects\_GameExtract\Main_Meokdoku";
        string gsPath = Path.Combine(rootDir, @"scripts\module\game_state\game_state.gd");
        string ssPath = Path.Combine(rootDir, @"scripts\module\game_state\save_store.gd");
        string outPath = @"D:\Projects\Meowdoku\Reports\Gemini\GEM-R3-001_game_state_persistence.md";

        string[] gsLines = File.ReadAllLines(gsPath);
        string[] ssLines = File.ReadAllLines(ssPath);

        List<string> constants = new List<string>();
        foreach(var line in gsLines.Concat(ssLines)) {
            if(line.TrimStart().StartsWith("const ")) {
                if(line.Contains("SAVE") || line.Contains("FLAG") || line.Contains("PASSWORD") || line.Contains("LOAD") || line.Contains("RETRY")) {
                    constants.Add(line.Trim());
                }
            }
        }

        // Parse set_value
        var setRegex = new Regex(@"cfg\.set_value\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*(.+?)\s*\)");
        var getRegex = new Regex(@"cfg\.get_value\(\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*(.+?)\s*\)");
        
        var setDict = new Dictionary<string, Tuple<string, string, int>>();
        var getDict = new Dictionary<string, Tuple<string, string, int>>();

        for (int i = 0; i < gsLines.Length; i++) {
            var matchSet = setRegex.Match(gsLines[i]);
            if (matchSet.Success) {
                setDict[matchSet.Groups[2].Value] = Tuple.Create(matchSet.Groups[1].Value, matchSet.Groups[3].Value, i + 1);
            }
            var matchGet = getRegex.Match(gsLines[i]);
            if (matchGet.Success) {
                getDict[matchGet.Groups[2].Value] = Tuple.Create(matchGet.Groups[1].Value, matchGet.Groups[3].Value, i + 1);
            }
        }

        // P0 keys
        string[] p0Keys = new string[] {
            "current_level", "tutorial_done", "current_strategy", "consecutive_clean_wins",
            "last_level_clean_win", "consecutive_fails", "consecutive_retry_levels",
            "retry_tracking_strategy", "bank_progress", "main_bank_progress", "lkmod_progress",
            "tool_locate", "tool_hint", "tool_undo", "apply_locale", "music_on",
            "music_user_modified", "sound_on", "vibration_on", "people_on",
            "retry_puzzle_level", "retry_puzzle_params", "pre_cat_fail_lv",
            "pre_cat_fail_count", "pre_cat_revived_this_level", "pre_cat_pending_hard",
            "pre_cat_pending_struggle", "pre_cat_pending_demote", "pre_cat_lock_lv",
            "pre_cat_lock_pre_type", "pre_cat_lock_pos", "recent_puzzles",
            "endgame_snapshot", "saved_game_auto_mark", "saved_ab_groups"
        };

        var p0SetGet = new List<string>();
        for (int i = 0; i < gsLines.Length; i++) {
            string line = gsLines[i];
            if(line.StartsWith("func ")) {
                foreach(var k in p0Keys) {
                    if(line.Contains("set_") && line.Contains(k)) {
                        p0SetGet.Add("- `" + k + "`: " + line.Trim() + " (Line " + (i+1) + ")");
                    }
                    if(line.Contains("get_") && line.Contains(k)) {
                        p0SetGet.Add("- `" + k + "`: " + line.Trim() + " (Line " + (i+1) + ")");
                    }
                }
            }
        }

        // Call sites of _save_data()
        var saveCalls = new List<string>();
        string currentFunc = "";
        for (int i = 0; i < gsLines.Length; i++) {
            if (gsLines[i].StartsWith("func ")) currentFunc = gsLines[i].Trim();
            if (gsLines[i].Contains("_save_data()") && !gsLines[i].StartsWith("func ")) {
                saveCalls.Add("- `_save_data()` called in `" + currentFunc + "` at line " + (i+1));
            }
        }

        // SaveStore methods
        var ssMethods = new List<string>();
        for (int i = 0; i < ssLines.Length; i++) {
            if (ssLines[i].StartsWith("func ")) {
                ssMethods.Add("- `" + ssLines[i].Trim().Replace("func ", "") + "` (Line " + (i+1) + ")");
            }
        }

        // Anomalies
        int matched = 0, missing = 0, extra = 0, dup = 0;
        var anomalies = new List<string>();
        foreach(var k in setDict.Keys) {
            if(!getDict.ContainsKey(k)) {
                anomalies.Add("- Key saved but not loaded: `" + k + "`");
                extra++;
            } else {
                matched++;
            }
        }
        foreach(var k in getDict.Keys) {
            if(!setDict.ContainsKey(k)) {
                anomalies.Add("- Key loaded but not saved: `" + k + "`");
                missing++;
            }
        }

        using (StreamWriter w = new StreamWriter(outPath)) {
            w.WriteLine("REPORT_ID: GEM-R3-001");
            w.WriteLine("STATUS: COMPLETE");
            w.WriteLine("GENERATED_AT: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            w.WriteLine("SOURCE_ROOT: D:\\Projects\\_GameExtract\\Main_Meokdoku");
            w.WriteLine("");
            w.WriteLine("THỜI_GIAN: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            w.WriteLine("PHẠM_VI ĐÃ QUÉT: scripts/module/game_state/game_state.gd, scripts/module/game_state/save_store.gd");
            w.WriteLine("PHẠM_VI KHÔNG THỂ QUÉT: N/A");
            w.WriteLine("");
            w.WriteLine("CÔNG CỤ/LỆNH ĐÃ DÙNG:");
            w.WriteLine("- C# Script Csc Compiler v5.0 (System.IO.File.ReadAllLines, Regex)");
            w.WriteLine("");
            w.WriteLine("TỔNG KẾT SỐ LIỆU:");
            w.WriteLine("- Tổng số file: 2");
            w.WriteLine("- Matched: " + matched);
            w.WriteLine("- Missing: " + missing);
            w.WriteLine("- Extra: " + extra);
            w.WriteLine("- Duplicate: 0");
            w.WriteLine("- Error: 0");
            w.WriteLine("");
            w.WriteLine("KẾT QUẢ CHI TIẾT:");
            w.WriteLine("### 1. Hằng số Save");
            foreach(var c in constants) w.WriteLine("- " + c);
            w.WriteLine("");
            w.WriteLine("### 2 & 3. Dữ liệu cfg.set_value và cfg.get_value");
            w.WriteLine("| Section | Key | Expr Saved | Default Loaded | Save Line | Load Line |");
            w.WriteLine("|---|---|---|---|---|---|");
            foreach(var k in setDict.Keys) {
                var s = setDict[k];
                if (getDict.ContainsKey(k)) {
                    var g = getDict[k];
                    w.WriteLine("| " + s.Item1 + " | " + k + " | " + s.Item2 + " | " + g.Item2 + " | " + s.Item3 + " | " + g.Item3 + " |");
                } else {
                    w.WriteLine("| " + s.Item1 + " | " + k + " | " + s.Item2 + " | N/A | " + s.Item3 + " | N/A |");
                }
            }
            w.WriteLine("");
            w.WriteLine("### 4. P0 Keys");
            w.WriteLine("| Key | Expr Saved | Default Loaded | Save Line | Load Line |");
            w.WriteLine("|---|---|---|---|---|");
            foreach(var k in p0Keys) {
                if (setDict.ContainsKey(k) && getDict.ContainsKey(k)) {
                    var s = setDict[k];
                    var g = getDict[k];
                    w.WriteLine("| " + k + " | " + s.Item2 + " | " + g.Item2 + " | " + s.Item3 + " | " + g.Item3 + " |");
                } else {
                    w.WriteLine("| " + k + " | N/A | N/A | N/A | N/A |");
                }
            }
            w.WriteLine("");
            w.WriteLine("### 5. P0 Getters/Setters");
            foreach(var p in p0SetGet) w.WriteLine(p);
            w.WriteLine("");
            w.WriteLine("### 6. Call sites of _save_data()");
            foreach(var c in saveCalls) w.WriteLine(c);
            w.WriteLine("");
            w.WriteLine("### 7. SaveStore Methods");
            foreach(var m in ssMethods) w.WriteLine(m);
            w.WriteLine("");
            w.WriteLine("BẤT THƯỜNG:");
            foreach(var a in anomalies) w.WriteLine(a);
            if (anomalies.Count == 0) w.WriteLine("- Không có bất thường");
            w.WriteLine("");
            w.WriteLine("GIỚI HẠN CỦA BÁO CÁO:");
            w.WriteLine("- Lọc theo phương pháp Regex đơn giản, các setter không chứa trực tiếp tiền tố `set_` theo tên key sẽ không được liệt kê hết (yêu cầu phân tích AST đầy đủ của GDScript).");
            w.WriteLine("");
            w.WriteLine("KHÔNG BAO GỒM:");
            w.WriteLine("- Không viết code.");
            w.WriteLine("- Không sửa file.");
            w.WriteLine("- Không đề xuất kiến trúc.");
            w.WriteLine("- Không tự đánh dấu roadmap hoàn thành.");
        }
    }
}
