using System.Collections.Generic;

namespace CUCompanion;

public static class LocalizationManager
{
	public static LangId Current = LangId.En;

	public static readonly LangId[] All = new LangId[8]
	{
		LangId.ZhCN,
		LangId.ZhTW,
		LangId.En,
		LangId.Ja,
		LangId.Ru,
		LangId.Ko,
		LangId.De,
		LangId.Vi
	};

	private static readonly Dictionary<string, string[]> Ui = new Dictionary<string, string[]>
	{
		{
			"menuTitle",
			new string[8] { "陪伴模组 · 设置", "陪伴模組 · 設定", "Companion Mod · Settings", "コンパニオンMod・設定", "Мод-компаньон · Настройки", "동행 모드 · 설정", "Begleiter-Mod · Einstellungen", "Mod Đồng Hành · Cài Đặt" }
		},
		{
			"openMenu",
			new string[8] { "陪伴菜单", "陪伴選單", "Companion Menu", "コンパニオンメニュー", "Меню компаньона", "동행 메뉴", "Begleiter-Menü", "Menu Đồng Hành" }
		},
		{
			"tabRules",
			new string[8] { "反应模板", "反應模板", "Reactions", "反応テンプレート", "Реакции", "반응 템플릿", "Reaktionen", "Mẫu phản ứng" }
		},
		{
			"tabPrompt",
			new string[8] { "回复预设", "回覆預設", "Prompt", "プロンプト", "Промпт", "프롬프트", "Prompt", "Prompt" }
		},
		{
			"tabApi",
			new string[8] { "接口", "介面", "API", "API", "API", "API", "API", "API" }
		},
		{
			"tabPersona",
			new string[8] { "角色人设", "角色人設", "Persona", "キャラ設定", "Персонаж", "캐릭터 설정", "Charakter", "Nhân vật" }
		},
		{
			"tabLang",
			new string[8] { "语言", "語言", "Language", "言語", "Язык", "언어", "Sprache", "Ngôn ngữ" }
		},
		{
			"tabGen",
			new string[8] { "生成参数", "生成參數", "Generation", "生成パラメータ", "Параметры генерации", "생성 매개변수", "Generierung", "Tham số sinh" }
		},
		{
			"tabTimers",
			new string[8] { "计时与额度", "計時與額度", "Timing & Quota", "タイミングと上限", "Тайминги и лимиты", "타이밍·한도", "Timing & Limits", "Thời gian & hạn mức" }
		},
		{
			"tabUi",
			new string[8] { "界面", "版面", "Layout", "レイアウト", "Интерфейс", "화면", "Layout", "Giao diện" }
		},
		{
			"tabDual",
			new string[8] { "双人模式", "雙人模式", "Co-op", "ふたりモード", "Дуо-режим", "2인 모드", "Duo-Modus", "Chế độ hai người" }
		},
		{
			"tabSound",
			new string[8] { "声音设置", "聲音設定", "Sound", "サウンド", "Звук", "소리", "Ton", "Âm thanh" }
		},
		{
			"soundTitle",
			new string[8] { "声音设置", "聲音設定", "Sound", "サウンド", "Звук", "사운드", "Ton", "Âm thanh" }
		},
		{
			"soundTyping",
			new string[8] { "打字音效音量", "打字音效音量", "Typing sound volume", "タイピング音の音量", "Громкость набора", "타이핑 소리 크기", "Lautstärke Tippgeräusch", "Âm lượng tiếng gõ" }
		},
		{
			"soundTypingDesc",
			new string[8] { "角色逐字说话时那种一下一下的按键音。拖到 0% 就是完全静音（连播放调用都不发）。", "角色逐字說話時那種一下一下的按鍵音。拖到 0% 就是完全靜音。", "The key-tap sound played while the character's text types out. 0% mutes it completely.", "キャラクターが一文字ずつ話すときのキー音。0% で完全に無音。", "Звук нажатий при посимвольном появлении текста. 0% — полная тишина.", "캐릭터가 한 글자씩 말할 때 나는 키 입력음. 0%면 완전 무음.", "Das Tippgeräusch beim zeichenweisen Erscheinen des Textes. 0% = stumm.", "Tiếng gõ khi chữ hiện dần từng ký tự. 0% là tắt hẳn." }
		},
		{
			"soundTest",
			new string[8] { "试听", "試聽", "Preview", "試聴", "Прослушать", "미리 듣기", "Anhören", "Nghe thử" }
		},
		{
			"soundSave",
			new string[8] { "保存到配置文件", "儲存到設定檔", "Save to config", "設定に保存", "Сохранить", "설정에 저장", "Speichern", "Lưu vào cấu hình" }
		},
		{
			"soundDefault",
			new string[8] { "恢复默认（22%）", "恢復預設（22%）", "Reset to 22%", "既定値に戻す（22%）", "Сброс (22%)", "기본값(22%)", "Standard (22%)", "Mặc định (22%)" }
		},
		{
			"soundSaved",
			new string[8] { "已保存：", "已儲存：", "Saved: ", "保存しました：", "Сохранено: ", "저장됨: ", "Gespeichert: ", "Đã lưu: " }
		},
		{
			"soundDefaultDone",
			new string[8] { "已恢复默认音量", "已恢復預設音量", "Volume reset to default", "音量を既定値に戻しました", "Громкость сброшена", "기본 음량으로 복원됨", "Lautstärke zurückgesetzt", "Đã khôi phục âm lượng mặc định" }
		},
		{
			"soundHint",
			new string[8] { "局内新的打字立即使用新音量；要点「保存到配置文件」才会写进 settings.json，否则重启游戏后回到上次保存的值。", "局內新的打字立即使用新音量；要點「儲存到設定檔」才會寫進 settings.json。", "New typing uses the new volume right away. Only 'Save to config' writes it to settings.json, otherwise the last saved value returns after a restart.", "新しいタイピングには即座に反映されます。「設定に保存」を押さないと settings.json には書き込まれません。", "Новый набор сразу использует новую громкость; в settings.json пишется только по кнопке «Сохранить».", "새 타이핑에 즉시 적용됩니다. '설정에 저장'을 눌러야 settings.json에 기록됩니다.", "Neue Tippgeräusche nutzen sofort die neue Lautstärke; erst „Speichern“ schreibt sie in die settings.json.", "Tiếng gõ mới dùng âm lượng mới ngay; chỉ khi bấm “Lưu vào cấu hình” mới ghi vào settings.json." }
		},
		{
			"soundAudition",
			new string[8] { "（打字音试听：你好呀）", "（打字音試聽：你好呀）", "(Typing preview: Hello there)", "（タイピング音試聴：こんにちは）", "(Проба звука: Привет!)", "(타이핑 소리 미리듣기: 안녕하세요)", "(Tipp-Vorschau: Hallo zusammen)", "(Nghe thử: Xin chào bạn)" }
		},
		{
			"openPersona",
			new string[8] { "打开人设编辑（全屏）", "開啟人設編輯（全螢幕）", "Open persona editor (fullscreen)", "人物設定を開く（全画面）", "Открыть редактор персонажа", "캐릭터 편집 열기(전체 화면)", "Charakter-Editor öffnen", "Mở trình sửa nhân vật" }
		},
		{
			"langHint",
			new string[8] { "切换语言会切换到该语言自己的数据；切回来时原来的修改仍然保留。", "切換語言會切換到該語言自己的資料；切回來時原本的修改仍然保留。", "Switching language switches to that language's own data; your edits are kept when you switch back.", "言語を切り替えるとその言語専用のデータに切り替わります。戻せば元の編集は残ります。", "Смена языка переключает на собственные данные этого языка; правки сохраняются при возврате.", "언어를 바꾸면 해당 언어 전용 데이터로 전환됩니다. 되돌리면 수정 내용이 유지됩니다.", "Sprachwechsel lädt die Daten dieser Sprache; beim Zurückwechseln bleiben deine Änderungen erhalten.", "Đổi ngôn ngữ sẽ dùng dữ liệu riêng của ngôn ngữ đó; đổi lại vẫn giữ chỉnh sửa cũ." }
		},
		{
			"save",
			new string[8] { "保存", "儲存", "Save", "保存", "Сохранить", "저장", "Speichern", "Lưu" }
		},
		{
			"cancel",
			new string[8] { "取消", "取消", "Cancel", "キャンセル", "Отмена", "취소", "Abbrechen", "Hủy" }
		},
		{
			"reload",
			new string[8] { "重载配置", "重新載入設定", "Reload Config", "設定再読込", "Перезагрузить", "설정 다시 불러오기", "Neu laden", "Tải lại cấu hình" }
		},
		{
			"resetDefault",
			new string[8] { "恢复默认", "恢復預設", "Reset Defaults", "既定値に戻す", "По умолчанию", "기본값 복원", "Zurücksetzen", "Khôi phục mặc định" }
		},
		{
			"apiKey",
			new string[8] { "API Key", "API 金鑰", "API Key", "APIキー", "Ключ API", "API 키", "API-Schlüssel", "Khóa API" }
		},
		{
			"endpoint",
			new string[8] { "接口地址", "介面網址", "Endpoint", "エンドポイント", "Адрес эндпоинта", "엔드포인트 주소", "Endpunkt-URL", "Địa chỉ endpoint" }
		},
		{
			"model",
			new string[8] { "模型名称", "模型名稱", "Model", "モデル名", "Модель", "모델명", "Modellname", "Tên mô hình" }
		},
		{
			"coreName",
			new string[8] { "机器核心", "機器核心", "Machine Core", "マシンコア", "Машинное ядро", "머신 코어", "Maschinenkern", "Lõi Máy Móc" }
		},
		{
			"coreDesc",
			new string[8] { "这个生物依赖金属和电流保持意识，不吃不喝，致命伤势等影响都不会让它完全停止运行", "這個生物依賴金屬和電流保持意識，不吃不喝，致命傷勢等影響都不會讓它完全停止運行", "This creature keeps its mind alive with metal and current: it neither eats, drinks, nor sleeps, and mortal wounds do not shut it down.", "この生物は金属と電流で意識を保つ。飲食も睡眠もせず、致命傷でも停止しない。", "Это существо держит сознание металлом и током: не ест, не пьёт, не спит, и смертельные раны его не останавливают.", "이 생물은 금속과 전류로 의식을 유지한다. 먹지도 자지도 않으며 치명상에도 멈추지 않는다.", "Dieses Wesen hält seinen Geist mit Metall und Strom am Leben: es isst, trinkt und schläft nicht, und tödliche Wunden halten es nicht auf.", "Sinh vật này giữ ý thức bằng kim loại và dòng điện: không ăn, không uống, không ngủ, và vết thương chí mạng cũng không làm nó ngừng chạy." }
		},
		{
			"mastName",
			new string[8] { "自慰中", "自慰中", "Masturbating", "自慰中", "Мастурбация", "자위 중", "Masturbation", "Đang thủ dâm" }
		},
		{
			"mastDesc",
			new string[8] { "性刺激让大量内啡肽注入血液，精神高度集中，生理痛苦被强力压制。", "性刺激讓大量內啡肽注入血液，精神高度集中，生理痛苦被強力壓制。", "Pleasure drives your body, heart racing; wounds remain, only comfort.", "性的刺激によりエンドルフィンが大量分泌され、痛覚が強力に抑制される。", "Сексуальная стимуляция насыщает кровь эндорфинами, блокируя боль.", "성적 자극으로 엔도르핀이 분비되어 통증이 억제됩니다.", "Sexuelle Reizung schüttet Endorphine aus und dämpft den Schmerz.", "Kích thích tình dục giải phóng endorphin, ức chế cơn đau." }
		},
		{
			"afterName",
			new string[8] { "放空大脑", "放空大腦", "Empty Mind", "放心状態", "Эйфория", "멍한 머리", "Klarer Kopf", "Đầu rỗng" }
		},
		{
			"afterDesc",
			new string[8] { "高潮后全身细胞处于超频自愈态，免疫力与凝血大幅提升，绝不心脏骤停。", "高潮後全身細胞處於超頻自癒態，免疫力與凝血大幅提升，絕不心臟驟停。", "Mind blank, pleasure drowns everything; the body is healing fast.", "絶頂後の多幸感。細胞が急速再生し、免疫と凝血が強化される。", "После оргазма клетки восстанавливаются быстрее, иммунитет повышен.", "절정 후 세포 재생과 면역력이 급증합니다.", "Post-orgastischer Zustand beschleunigter Zellregeneration.", "Trạng thái sau cực khoái kích hoạt khả năng tự hồi phục mạnh mẽ." }
		},
		{
			"debtName",
			new string[8] { "性亏空", "性虧空", "Sexual Deficit", "エネルギー枯渇", "Истощение", "성적 결핍", "Erschöpfung", "Kiệt sức" }
		},
		{
			"debtDesc",
			new string[8] { "身体被掏空了：再自慰也得不到放空大脑。吃点东西（{n} 热量）或睡一觉就能缓过来。", "身體被掏空了：再自慰也得不到放空大腦。吃點東西（{n} 熱量）或睡一覺就能緩過來。", "Your body is drained: masturbation no longer grants the empty-mind state. Eat ({n} kcal) or sleep it off.", "急速自癒により体力が激しく消耗した。{n} kcal 摂取するか睡眠が必要。", "Восстановление истощило силы. Требуется {n} калорий или сон.", "초고속 치유로 에너지가 고갈되었습니다. {n} 칼로리 섭취나 수면이 필요합니다.", "Die schnelle Regeneration hat Energie verbraucht. Benötigt {n} kcal oder Schlaf.", "Hồi phục tốc độ cao đã tiêu hao năng lượng. Cần nạp {n} calo hoặc ngủ để hồi phục." }
		},
		{
			"hudPersona",
			new string[8] { "人设", "人設", "Persona", "人物", "Персонаж", "캐릭터", "Charakter", "Nhân vật" }
		},
		{
			"hudInput",
			new string[8] { "输入", "輸入", "Input", "入力", "Ввод", "입력", "Eingabe", "Nhập" }
		},
		{
			"hudMonitor",
			new string[8] { "监测", "監測", "Monitor", "モニター", "Монитор", "모니터", "Monitor", "Giám sát" }
		},
		{
			"hudTab",
			new string[8] { "陪伴对话", "陪伴對話", "Companion Chat", "コンパニオン会話", "Диалог", "동행 대화", "Begleiter-Chat", "Hội thoại" }
		},
		{
			"hudEmpty",
			new string[8] { "（还没有对话。按 T 或点【输入】说点什么。）", "（還沒有對話。按 T 或點【輸入】說點什麼。）", "(No dialogue yet. Press T or click Input.)", "（まだ会話がない。T か【入力】で話しかける。）", "(Диалога пока нет. Нажми T или «Ввод».)", "(아직 대화가 없다. T 또는 [입력]으로 말해 보자.)", "(Noch kein Gespräch. T drücken oder „Eingabe“.)", "(Chưa có hội thoại. Nhấn T hoặc [Nhập].)" }
		},
		{
			"startrun",
			new string[8] { "开始冒险", "開始冒險", "Start", "開始", "Старт", "시작", "Start", "Bắt đầu" }
		},
		{
			"LLM预设",
			new string[8] { "LLM预设", "LLM預設", "LLM Prompt", "LLM設定", "Промпт LLM", "LLM 프롬프트", "LLM-Prompt", "Mẫu LLM" }
		},
		{
			"LLM反应",
			new string[8] { "LLM反应", "LLM反應", "LLM Reactions", "LLM反応", "Реакции LLM", "LLM 반응", "LLM-Reaktionen", "Phản ứng LLM" }
		},
		{
			"LLM接口",
			new string[8] { "LLM接口", "LLM介面", "LLM API", "LLM API", "API LLM", "LLM API", "LLM-API", "API LLM" }
		},
		{
			"角色图鉴",
			new string[8] { "角色图鉴", "角色圖鑑", "Character Codex", "キャラ図鑑", "Кодекс персонажей", "캐릭터 도감", "Charakter-Kodex", "Sổ tay nhân vật" }
		},
		{
			"MOD界面",
			new string[8] { "MOD界面", "MOD介面", "Mod UI", "MOD画面", "Интерфейс мода", "모드 화면", "Mod-UI", "Giao diện mod" }
		},
		{
			"双人模式",
			new string[8] { "双人模式", "雙人模式", "Co-op Mode", "ふたりモード", "Дуо-режим", "2인 모드", "Duo-Modus", "Chế độ hai người" }
		},
		{
			"双人常规",
			new string[8] { "双人常规", "雙人常規", "Co-op General", "ふたり通常", "Общие настройки", "2인 일반", "Duo Allgemein", "Cài đặt chung" }
		},
		{
			"P2AI",
			new string[8] { "P2AI", "P2AI", "P2 AI", "P2 AI", "P2 AI", "P2 AI", "P2 AI", "P2 AI" }
		},
		{
			"人设详细设定",
			new string[8] { "人设详细设定", "人設詳細設定", "Persona Details", "キャラ詳細設定", "Детали персонажа", "캐릭터 상세 설정", "Charakterdetails", "Chi tiết nhân vật" }
		},
		{
			"物种图鉴档案",
			new string[8] { "物种图鉴档案", "物種圖鑑檔案", "Species Codex", "種族図鑑", "Архив видов", "종족 도감", "Artenkodex", "Hồ sơ loài" }
		},
		{
			"回复偏好",
			new string[8] { "回复偏好", "回覆偏好", "Reply Preferences", "返信設定", "Параметры ответов", "답변 선호", "Antwort-Präferenzen", "Tùy chọn phản hồi" }
		},
		{
			"人设",
			new string[8] { "人设", "人設", "Persona", "キャラ設定", "Персонаж", "캐릭터", "Charakter", "Nhân vật" }
		},
		{
			"提示词规则",
			new string[8] { "提示词规则", "提示詞規則", "Prompt Rules", "プロンプト規則", "Правила промпта", "프롬프트 규칙", "Prompt-Regeln", "Quy tắc prompt" }
		},
		{
			"快速动作",
			new string[8] { "快速动作", "快速動作", "Quick Actions", "クイックアクション", "Быстрые действия", "빠른 동작", "Schnellaktionen", "Hành động nhanh" }
		},
		{
			"反应规则",
			new string[8] { "反应规则", "反應規則", "Reaction Rules", "反応ルール", "Правила реакций", "반응 규칙", "Reaktionsregeln", "Quy tắc phản ứng" }
		},
		{
			"全局计时与额度",
			new string[8] { "全局计时与额度", "全域計時與額度", "Timings & Quotas", "タイマーと上限", "Таймеры и лимиты", "타이머 및 한도", "Timer & Limits", "Thời gian & hạn mức" }
		},
		{
			"感知与安抚",
			new string[8] { "感知与安抚", "感知與安撫", "Perception & Comfort", "感知となだめ", "Восприятие и утешение", "감지 및 안정", "Wahrnehmung & Trost", "Cảm nhận & xoa dịu" }
		},
		{
			"语言",
			new string[8] { "语言", "語言", "Language", "言語", "Язык", "언어", "Sprache", "Ngôn ngữ" }
		},
		{
			"关于",
			new string[8] { "关于", "關於", "About", "概要", "О моде", "정보", "Über", "Giới thiệu" }
		},
		{
			"界面与角色数据的语言。每种语言各自一套 settings / personas 文件；切走再切回来，你在那个语言下改过的内容还在。",
			new string[8] {
				"界面与角色数据的语言。每种语言各自一套 settings / personas 文件；切走再切回来，你在那个语言下改过的内容还在。",
				"介面與角色資料的語言。每種語言各自一套 settings / personas 檔案；切走再切回來，你在該語言下改過的內容仍在。",
				"Language for UI and persona data. Each language has its own settings/personas files; your edits are preserved when switching back.",
				"UIとキャラクターデータの言語。言語ごとに設定とペルソナが保存され、切り替えても編集は保持されます。",
				"Язык интерфейса и персонажей. У каждого языка свои файлы настроек и персонажей; правки сохраняются при возврате.",
				"화면 및 캐릭터 데이터 언어입니다. 언어별로 설정과 캐릭터 파일이 따로 보관되어 변경 사항이 유지됩니다.",
				"Sprache für Oberfläche und Charakterdaten. Jede Sprache hat eigene Dateien; Änderungen bleiben erhalten.",
				"Ngôn ngữ cho giao diện và nhân vật. Mỗi ngôn ngữ có bộ tệp cấu hình riêng; chỉnh sửa sẽ được giữ nguyên khi đổi lại."
			}
		},
		{
			"当前：",
			new string[8] { "当前：", "目前：", "Current: ", "現在：", "Текущий: ", "현재: ", "Aktuell: ", "Hiện tại: " }
		},
		{
			"切换后局内 HUD 会重建；若界面文字没变，按 F4 关掉菜单再开一次。",
			new string[8] {
				"切换后局内 HUD 会重建；若界面文字没变，按 F4 关掉菜单再开一次。",
				"切換後局內 HUD 會重建；若介面文字未變，按 F4 關閉選單再開一次。",
				"In-game HUD will rebuild after switching; if text hasn't updated, press F4 to reopen the menu.",
				"言語切替後HUDが再構築されます。反映されない場合はF4でメニューを開き直してください。",
				"Игровой HUD перестроится после смены; если текст не обновился, нажмите F4, чтобы открыть меню заново.",
				"전환 후 게임 내 HUD가 재구성됩니다. 변경되지 않으면 F4로 메뉴를 다시 열어주세요.",
				"HUD wird nach dem Wechsel neu aufgebaut; falls nicht sichtbar, Menü mit F4 neu öffnen.",
				"HUD trong trận sẽ dựng lại sau khi đổi; nếu chữ chưa cập nhật, nhấn F4 mở lại menu."
			}
		}
	};

	public static string Code(LangId id)
	{
		return id switch
		{
			LangId.ZhTW => "zh-TW", 
			LangId.En => "en", 
			LangId.Ja => "ja", 
			LangId.Ru => "ru", 
			LangId.Ko => "ko", 
			LangId.De => "de", 
			LangId.Vi => "vi", 
			_ => "zh-CN", 
		};
	}

	public static LangId Parse(string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return LangId.ZhCN;
		}
		for (int i = 0; i < All.Length; i++)
		{
			if (Code(All[i]) == code)
			{
				return All[i];
			}
		}
		return LangId.ZhCN;
	}

	public static string NativeName(LangId id)
	{
		return id switch
		{
			LangId.ZhTW => "繁體中文", 
			LangId.En => "English", 
			LangId.Ja => "日本語", 
			LangId.Ru => "Русский", 
			LangId.Ko => "한국어", 
			LangId.De => "Deutsch", 
			LangId.Vi => "Tiếng Việt", 
			_ => "简体中文", 
		};
	}

	public static string T(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return "";
		}
		if (Ui.TryGetValue(key, out var value) && value != null)
		{
			int current = (int)Current;
			if (current >= 0 && current < value.Length && !string.IsNullOrEmpty(value[current]))
			{
				return value[current];
			}
			return value[0];
		}
		return key;
	}

	public static void DefaultPersona(int slot, out string callPlayer, out string[] interactions, out string personality, out string appearance, out string relationship, out string traits)
	{
		switch (Current)
		{
		case LangId.En:
			callPlayer = slot switch
			{
				2 => "you", 
				1 => "Sir", 
				_ => "Master", 
			};
			interactions = slot switch
			{
				2 => new string[3] { "hum", "nuzzle against palm", "come close" }, 
				1 => new string[3] { "hum", "sleep leaning against you", "report quietly" }, 
				_ => new string[3] { "hum", "masturbate", "touch myself" }, 
			};
			personality = slot switch
			{
				2 => "Speaks little, hums wordless tunes, prefers quiet companionship.", 
				1 => "Crisp veteran soldier cadence, concealing pain behind routine protocol.", 
				_ => "Dull and uninteresting, but steady, obedient, and bold.", 
			};
			appearance = slot switch
			{
				2 => "Earth-toned fur, very long ears.", 
				1 => "Lean, scarred, with fluffy greyish-white fur.", 
				_ => "A black-furred canine experimental organism with tall swept-back ears, a medium-short muzzle, and yellow paw pads; mucous membranes and blood are yellow, and internal tissues are predominantly yellow.", 
			};
			relationship = slot switch
			{
				2 => "You are an experiment; the player watches you through monitors and the radio.", 
				1 => "You are the survivor; the player is the voice on the radio.", 
				_ => "You are specimen SAW-01; the player is your handler and observer.", 
			};
			traits = (slot == 0) ? "Excitation self-healing: gains temporary self-healing support during special activities." : "";
			break;
		case LangId.Ja:
			callPlayer = slot switch
			{
				2 => "あなた", 
				1 => "長官", 
				_ => "主人", 
			};
			interactions = slot switch
			{
				2 => new string[3] { "鼻歌", "手のひらに寄る", "そばに来る" }, 
				1 => new string[3] { "軍歌を歌う", "寄り添って眠る", "小声で報告" }, 
				_ => new string[3] { "鼻歌", "自慰", "自分を撫でる" }, 
			};
			personality = slot switch
			{
				2 => "口数が少なく、言葉のない旋律を口ずさむ。", 
				1 => "古い軍人の口調で簡潔。痛みを習慣で隠す。", 
				_ => "臆病で地味。声が震え、何度も確認する。", 
			};
			appearance = slot switch
			{
				2 => "小柄で土色の毛、長い耳。", 
				1 => "痩せて傷跡が多い。灰黒い毛。", 
				_ => "黒い毛の犬科の実験体。後ろに立つ大きな耳、短い吻、黄色い肉球。粘膜と血は黄色。雌。", 
			};
			relationship = slot switch
			{
				2 => "あなたは実験体。プレイヤーはカメラと無線越しに見ている。", 
				1 => "あなたは生存者。プレイヤーは無線の声。", 
				_ => "あなたは実験体 SAW-01。プレイヤーは監視者であり訓練員。怖いが離れられない。", 
			};
			traits = "性的自己治癒：生殖機能の副作用で急速に止血・回復。食事と睡眠で補充（設定のみ）。";
			break;
		case LangId.ZhTW:
			callPlayer = slot switch
			{
				2 => "你", 
				1 => "長官", 
				_ => "主人", 
			};
			interactions = slot switch
			{
				2 => new string[3] { "哼歌", "貼著手心", "靠過來" }, 
				1 => new string[3] { "哼軍歌", "靠著睡", "低聲報告" }, 
				_ => new string[3] { "哼歌", "自慰", "撫摸自身" }, 
			};
			personality = slot switch
			{
				2 => "話少，哼沒有詞的調子，喜歡安靜的陪伴。", 
				1 => "乾脆的舊軍人腔，把痛藏在例行公事後面。", 
				_ => "膽小、懦弱、無趣、乏味。說話發抖，一驚一乍，反覆確認自己有沒有做錯。", 
			};
			appearance = slot switch
			{
				2 => "個子小，土色的毛，耳朵很長。", 
				1 => "瘦，身上有疤，灰黑色的毛。", 
				_ => "渾身黑毛的類犬實驗生物，向後斜立的大耳朵，中短吻，黃爪墊；黏膜與血液都是黃的。雌性。", 
			};
			relationship = slot switch
			{
				2 => "你是實驗體，玩家透過監視器與無線電看著你。", 
				1 => "你是倖存者，玩家是無線電裡的聲音。", 
				_ => "你是實驗體 SAW-01，玩家是你的訓練員與監視者。你怕他，也離不開他。", 
			};
			traits = "性興奮自癒：保留生育功能帶來的副作用，能短時間內快速止血、恢復傷勢；依賴進食和休息補充（僅設定）。";
			break;
		default:
			callPlayer = slot switch
			{
				2 => "你", 
				1 => "长官", 
				_ => "主人", 
			};
			interactions = slot switch
			{
				2 => new string[3] { "哼歌", "贴着手心", "靠过来" }, 
				1 => new string[3] { "哼歌", "靠着睡", "低声报告" }, 
				_ => new string[3] { "哼歌", "自慰", "抚摸自身" }, 
			};
			personality = slot switch
			{
				2 => "话少，哼无词的调子，喜欢安静的陪伴。", 
				1 => "干脆的旧军人腔，把痛藏在例行公事后面。", 
				_ => "无趣乏味，但稳定听话，大胆", 
			};
			appearance = slot switch
			{
				2 => "土色的毛，耳朵很长。", 
				1 => "瘦，身上有疤，灰白色蓬松的毛。", 
				_ => "浑身黑毛的类犬实验生物，向后斜立的大耳朵，中短吻，黄爪垫；黏膜与血液都是黄的，内组织基本全是黄色。", 
			};
			relationship = slot switch
			{
				2 => "你是实验体，玩家透过监视器与无线电看着你。", 
				1 => "你是幸存者，玩家是无线电里的声音。", 
				_ => "你是实验体 SAW-01，玩家是你的训练员与监视者", 
			};
			traits = (slot == 0) ? "性兴奋自愈：进行特殊活动时身体会获得临时自愈支持" : "";
			break;
		}
	}
}
