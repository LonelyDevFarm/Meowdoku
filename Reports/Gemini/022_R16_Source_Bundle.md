# PROJECT: GODOT

## PATH: scripts/module/feedback/view/feedback_page.gd
``gdscript




class_name FeedbackPage
extends UIFrameWindow

signal closed

@onready var _text_edit: TextEdit = $Root / Content / Card / ContentArea / InputText
@onready var _submit_btn: Button = $Root / Content / Card / SubmitBtn
@onready var _submit_btn_disabled: Button = $Root / Content / Card / SubmitBtnDisabled
@onready var _anim: AnimationPlayer = $Root / AnimationPlayer

var _closing: bool = false
var _as_dlg: bool = false
var is_submitted: bool = false

func _ready() -> void :
	_text_edit.text_changed.connect(_on_input_text_changed)
	_update_submit_state()
	bind_press_release_scale($Root / Content / Card / CloseBtn)

func on_show(_params: Dictionary = {}) -> void :
	_as_dlg = _params.get("as_dlg", false)
	_text_edit.text = ""
	_update_submit_state()
	_closing = false
	is_submitted = false
	_anim.play_section_with_markers("GenericPopup", &"", &"Mark")



func _input(event: InputEvent) -> void :
	if not _text_edit.has_focus():
		return
	var is_press: bool = (event is InputEventScreenTouch and event.pressed)\
	or (event is InputEventMouseButton and event.pressed)
	if not is_press:
		return
	if not _text_edit.get_global_rect().has_point(event.position):
		_text_edit.release_focus()
		if DisplayServer.has_feature(DisplayServer.FEATURE_VIRTUAL_KEYBOARD):
			DisplayServer.virtual_keyboard_hide()

func _on_input_text_changed() -> void :
	_update_submit_state()

func _on_submit_pressed() -> void :
	var content: String = _text_edit.text.strip_edges()
	if content.is_empty():
		return
	Tracker.track_btn_click(Tracker.Btn.SUBMIT, self, {"feedback_record": content})
	print("[FeedbackPage] ç”¨æˆ·åé¦ˆ: ", content)

	Toast.popup("%s\n%s" % [tr("FEEDBACK_TOAST_THANKS_TITLE"), tr("FEEDBACK_TOAST_THANKS_DESC")], self)
	is_submitted = true
	await _close_with_anim()
	closed.emit()


func _on_close_pressed() -> void :
	Tracker.track_btn_click(Tracker.Btn.CLOSE, self)
	await _close_with_anim()
	closed.emit()


func _close_with_anim() -> void :
	if _closing:
		return
	_closing = true
	_anim.play_section_with_markers("GenericPopup", &"Mark", &"")
	await _anim.animation_finished



func _update_submit_state() -> void :
	var has_text: bool = not _text_edit.text.strip_edges().is_empty()
	_submit_btn.visible = has_text
	_submit_btn_disabled.visible = not has_text


func get_scr_name() -> String:
	return "" if _as_dlg else Tracker.Scr.FEEDBACK


func get_dlg_name() -> String:
	return Tracker.Dlg.FEEDBACK if _as_dlg else ""

``

## PATH: scripts/module/rate_us/view/rate_us_page.gd
``gdscript
class_name RateUsPage
extends UIFrameWindow

signal closed(data: Dictionary)

@onready var _star1: TextureRect = $Root / Content / Dialog / StarsRow / Star1
@onready var _star2: TextureRect = $Root / Content / Dialog / StarsRow / Star2
@onready var _star3: TextureRect = $Root / Content / Dialog / StarsRow / Star3
@onready var _star4: TextureRect = $Root / Content / Dialog / StarsRow / Star4
@onready var _star5: TextureRect = $Root / Content / Dialog / StarsRow / Star5
@onready var _anim: AnimationPlayer = $Root / AnimationPlayer

var _stars: Array[TextureRect]
var _star_lit_tex: Texture2D
var _star_dim_tex: Texture2D
var _selected_stars: int = 5
var _closing: bool = false
var _dragging: bool = false

func _ready() -> void :

	_stars = [_star1, _star2, _star3, _star4, _star5]

	_star_lit_tex = _star1.texture
	_star_dim_tex = _star4.texture
	for i in range(_stars.size()):
		var idx: = i
		_stars[i].gui_input.connect( func(event: InputEvent) -> void :
			if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
				if event.pressed:
					_dragging = true
					_select_stars(idx + 1)
				else:
					_dragging = false
		)
	bind_press_release_scale($Root / Content / Dialog / CloseBtn)

func _input(event: InputEvent) -> void :
	if not _dragging:
		return
	if event is InputEventMouseButton and not event.pressed:
		_dragging = false
		return
	if event is InputEventMouseMotion:
		var star_idx: int = _star_index_at(event.global_position)
		if star_idx >= 0:
			_select_stars(star_idx + 1)

func _star_index_at(global_pos: Vector2) -> int:
	for i in range(_stars.size()):
		var rect: Rect2 = _stars[i].get_global_rect()
		if rect.has_point(global_pos):
			return i
	return -1

func on_show(_params: Dictionary = {}) -> void :


	_closing = false
	_select_stars(5)
	_anim.play_section_with_markers(_get_anim_name(), &"", &"Mark")

func _select_stars(n: int) -> void :
	_selected_stars = n
	for i in range(_stars.size()):
		_stars[i].texture = _star_lit_tex if i < n else _star_dim_tex



func _on_close_btn_pressed() -> void :
	Tracker.track_btn_click(Tracker.Btn.CLOSE, self)
	await _close_with_anim()
	closed.emit({"star_count": 0, "is_submitted": false})


func _on_rate_us_btn_pressed() -> void :
	Tracker.track_btn_click(Tracker.Btn.RATE_US, self, {"rate_star": _selected_stars})
	await _close_with_anim()
	closed.emit({"star_count": _selected_stars, "is_submitted": true})


func _close_with_anim() -> void :
	if _closing:
		return
	_closing = true
	_anim.play_section_with_markers(_get_anim_name(), &"Mark", &"")
	await _anim.animation_finished


func _get_anim_name() -> StringName:
	return &"GenericPopup"



func get_hide_anim_duration() -> float:
	if _anim == null:
		return 0.0
	var n: = _get_anim_name()
	if not _anim.has_animation(n):
		return 0.0
	var a: = _anim.get_animation(n)
	return maxf(0.0, a.length - (a.get_marker_time(&"Mark") if a.has_marker(&"Mark") else 0.0))


func get_dlg_name() -> String:
	return Tracker.Dlg.RATE



func get_dlg_extra() -> Dictionary:
	return {"dlg_star_ui": "dlg_star_ui_0"}

``

## PATH: scripts/module/rate_us/view/rate_us_page_v2.gd
``gdscript
class_name RateUsPageV2
extends RateUsPage











const _STAR_FILL_AT: float = 0.3


func _get_anim_name() -> StringName:
	return &"GenericPopupV2"


func on_show(_params: Dictionary = {}) -> void :
	_closing = false
	_select_stars(0)
	_anim.play_section_with_markers(_get_anim_name(), &"", &"Mark")
	while _anim.is_playing() and _anim.current_animation_position < _STAR_FILL_AT:
		await get_tree().process_frame
	_select_stars(5)






func get_dlg_extra() -> Dictionary:
	return {"dlg_star_ui": "dlg_star_ui_1"}

``

## PATH: scripts/module/common/in_app_review_manager.gd
``gdscript








class_name InAppReviewManager
extends RefCounted


static func _plugin() -> Object:
	if Engine.has_singleton("InAppReviewPlugin"):
		return Engine.get_singleton("InAppReviewPlugin")
	return null







static func request_review() -> void :
	if OS.has_feature("ios"):


		UniKitManager.request_store_review()
		return
	var p: Object = _plugin()
	if p == null:
		return
	p.requestReview()

``

## PATH: scripts/common/helpshift_manager.gd
``gdscript



extends Node


signal unread_count_changed(count: int)


const ANDROID_APP_ID: String = "arsenal-support_platform_20260610074440920-419e5d01b34cf98"
const IOS_PLATFORM_ID: String = "arsenal-support_platform_20260610074440901-cc1ce66e7ed9026"
const IOS_API_KEY: String = "f6e712714ec70365ca39e75ec59799f2"
const DOMAIN: String = "arsenal-support.helpshift.com"


const ACTIVE_WINDOW_SEC: int = 2 * 86400


const DOT_HELPSHIFT_UNREAD: String = "helpshift_unread"

var _is_active: bool = false
var _unread_count: int = 0


func _plugin() -> Object:
	if Engine.has_singleton("HelpshiftPlugin"):
		return Engine.get_singleton("HelpshiftPlugin")
	return null



func _notification(what: int) -> void :
	if what == NOTIFICATION_APPLICATION_FOCUS_IN:
		request_unread()



func preheat() -> void :
	if _is_active:
		return
	var last: int = GameState.get_help_last_open_time()
	if last <= 0 or (_now() - last) > ACTIVE_WINDOW_SEC:
		return
	_install()
	if _is_active:
		request_unread()



func open_faq() -> void :
	if not _is_active:
		_install()
	var p: Object = _plugin()
	if p == null:
		return
	GameState.set_help_last_open_time(_now())
	p.showFAQs("ALWAYS", _build_metadata(), _build_cifs())


	request_unread()


func request_unread() -> void :
	if not _is_active:
		return
	var p: Object = _plugin()
	if p == null:
		return
	p.requestUnreadMessageCount(true)


func get_unread_count() -> int:
	return _unread_count



func _install() -> void :
	var p: Object = _plugin()
	if p == null:
		return
	if OS.has_feature("ios"):


		pass
	else:
		p.install(ANDROID_APP_ID, DOMAIN, false, false)
	if not p.is_connected("unread_message_count", _on_native_unread):
		p.connect("unread_message_count", _on_native_unread)
	if not p.is_connected("auth_failure", _on_native_auth_failure):
		p.connect("auth_failure", _on_native_auth_failure)
	_is_active = true


func _on_native_unread(count: int) -> void :
	_unread_count = count
	unread_count_changed.emit(count)

	RedDotCenter.set_count(DOT_HELPSHIFT_UNREAD, count)


func _on_native_auth_failure(reason: String) -> void :
	push_warning("[Helpshift] auth failure: %s" % reason)




func _build_metadata() -> Dictionary:
	var inner: Dictionary = UniKitManager.get_inner_tags()
	return {
		"uuid": UniKitManager.get_uuid(), 
		"luid": UniKitManager.get_luid(), 
		"hit_the_experimental_group": UniKitManager.get_ab_dyeing_tag(), 
		"country": UniKitManager.get_ab_country(), 
		"number_of_levels": GameState.get_current_level(), 
		"living_day": GameState.get_active_days(), 
		"locate_count": GameState.get_tool_count("locate"), 
		"hint_count": GameState.get_tool_count("hint"), 
		"install_version": GameState.get_install_version(), 
		"source_of_channels": inner.get("media_source", ""), 
		"flow_domain": inner.get("pm_flow_domain", ""), 
	}





func _build_cifs() -> Dictionary:
	var inner: Dictionary = UniKitManager.get_inner_tags()
	return {
		"uuid": {"type": "singleline", "value": UniKitManager.get_uuid()}, 
		"living_day": {"type": "singleline", "value": str(GameState.get_active_days())}, 
		"number_of_levels": {"type": "singleline", "value": str(GameState.get_current_level())}, 
		"hit_the_experimental_group": {"type": "multiline", "value": UniKitManager.get_ab_dyeing_tag()}, 
		"source_of_channels": {"type": "singleline", "value": str(inner.get("media_source", ""))}, 
		"install_version": {"type": "singleline", "value": GameState.get_install_version()}, 
	}


func _now() -> int:
	return int(Time.get_unix_time_from_system())

``

## PATH: scripts/module/abtest/config/rate_us_pop_config.gd
``gdscript
extends AbConfigBase
class_name RateUsPopConfig








const VALUE_GATE_LV8: String = "0"
const VALUE_GATE_LV15: String = "1"
const VALUE_HOME_AFTER_WIN: String = "2"
const VALUE_WIN_STREAK_5: String = "3"

func _init() -> void :
	key = "rate_us_pop"
	default_value = VALUE_GATE_LV8
	timing = ABTestManager.TIMING_GAME_START







func is_eligible_at_game_win(lv: int, session_consecutive_wins: int) -> bool:
	var v: String = value()
	match v:
		VALUE_GATE_LV8:
			return lv >= 8
		VALUE_WIN_STREAK_5:
			return lv >= 15 and session_consecutive_wins >= 5
	return false

``

## PATH: scripts/module/abtest/config/rate_us_pop_ui_config.gd
``gdscript
extends AbConfigBase
class_name RateUsPopUiConfig






const VALUE_OLD_UI: int = 0
const VALUE_NEW_UI: int = 1

func _init() -> void :
	key = "rate_us_pop_ui"
	default_value = VALUE_OLD_UI
	timing = ABTestManager.TIMING_GAME_START



func is_new_ui() -> bool:
	return value() == VALUE_NEW_UI

``

## PATH: scripts/module/result/view/game_win_page.gd (Partial)
``gdscript

  	for n: Node in [_title, _cat, $Root / Ctrl / VictoryCatGlow, _ray_light, $Root / Ctrl / EffectRayLight, $Root / 
Ctrl / EffectFlreworks, $Root / Ctrl / VBoxContainer]:
  		if n != null and n is CanvasItem:
  			(n as CanvasItem).visible = false
  
  
  
  
  
  
  func _restore_default_win_body() -> void :
  	if _pass_board != null:
  		_pass_board.visible = false
  
  	for n: Node in [_title, $Root / Ctrl / VictoryCatGlow, $Root / Ctrl / EffectFlreworks, $Root / Ctrl / 
VBoxContainer]:
  		if n != null and n is CanvasItem:
  			(n as CanvasItem).visible = true
  
  	for n: Node in [_cat, _ray_light, $Root / Ctrl / EffectRayLight]:
  		if n != null and n is CanvasItem:
  			(n as CanvasItem).visible = false
  
  
  
  
  
  
  
  
  func _run_post_win_popups(seq: int) -> void :
  	var lv: int = _level_config.get("level", 0)
> 	var will_show_rate_us: bool = _is_rate_us_eligible(lv)
  	var will_show_push: bool = _check_push_guide_eligible(lv)
  
> 	if will_show_rate_us or will_show_push:
  		UIManager.block_input_briefly(self, APPEAR_DURATION)
> 	if will_show_rate_us:
  		_next_btn.modulate.a = 0.0
  
  	await get_tree().create_timer(APPEAR_DURATION).timeout
  	if seq != _show_seq_id:
> 		if will_show_rate_us:
  			_restore_next_btn()
  		return
  
> 	if will_show_rate_us:
> 		await _show_rate_us(seq)
  		if seq != _show_seq_id:
  			return
  	await _show_push_guide(seq)
  
  
  
> func _is_rate_us_eligible(lv: int) -> bool:
> 	return ABTestManager.rate_us_pop.is_eligible_at_game_win(lv, GameState.get_session_consecutive_wins())\
> 	and not GameState.has_shown_rate_us()\
  	and UniKitManager.is_online()
  
  
  func _check_push_guide_eligible(lv: int) -> bool:
  	return PushGuideFlow.is_eligible(lv)
  
  
  
  
  
> func _show_rate_us(seq: int) -> void :
  	var lv: int = _level_config.get("level", 0)
> 	if not _is_rate_us_eligible(lv) or seq != _show_seq_id:
  		_restore_next_btn()
  		return
  
  
> 	GameState.mark_rate_us_shown()
> 	await _run_rate_us_flow()
  	_restore_next_btn()
  
> func _run_rate_us_flow() -> void :
  
  
  
> 	var page_key: StringName = UiName.RATE_US_V2 if ABTestManager.rate_us_pop_ui.is_new_ui() else UiName.RATE_US
> 	var rate_us: = UIManager.show_ui(page_key)
> 	var data: Dictionary = await rate_us.closed
  	UIManager.hide_ui(page_key)
  	if data.get("is_submitted") and data.get("star_count", 0) > 4:
  
  
  		InAppReviewManager.request_review()
  	elif data.get("is_submitted") and data.get("star_count", 0) <= 4:
> 		var feedback: = UIManager.show_ui(UiName.FEEDBACK, {"as_dlg": true})
> 		await feedback.closed
> 		UIManager.hide_ui(UiName.FEEDBACK)
  
  
  
  
  
  
  
  
  
  func _show_push_guide(seq: int) -> void :
  	var lv: int = _level_config.get("level", 0)
  	if not PushGuideFlow.is_eligible(lv):
  		return
  	if seq != _show_seq_id:
  		print("[Push][éªŒæ”¶] _show_push_guide: seq å·²è¿‡æœŸ(å¯èƒ½è¯„æ˜Ÿå¼¹çª—æœŸé—´é¡µé¢è¢« hide),ä¸­æ–­")
  		return
  	var ask_count: int = GameState.get_push_ask_count()
  	var show_count: int = GameState.get_push_guide_popup_count() + 1
  	print("[Push][éªŒæ”¶] _show_push_guide: push_ask_count=%d,å±•ç¤º pre_push_guide_page,show_count=%d" % [ask_count, 
show_count])
  	var page: = UIManager.show_ui(UiName.PRE_PUSH_GUIDE, {"show_count": show_count}) as PrePushGuidePage
  	var source: PrePushGuidePage.CloseSource = await page.closed
  	print("[Push][éªŒæ”¶] _show_push_guide: ç”¨æˆ·å…³é—­æ–¹å¼=%s" %
  		("ALLOW_BTN(å»å¼€å¯)" if source == PrePushGuidePage.CloseSource.ALLOW_BTN else "CLOSE_BTN(å…³é—­)"))
  	UIManager.hide_ui(UiName.PRE_PUSH_GUIDE)
  	if source == PrePushGuidePage.CloseSource.ALLOW_BTN:
  		if ask_count < 2:
  
  			print("[Push][éªŒæ”¶] _show_push_guide: ask_count<2,èµ° SYSTEM_AND_SETTING")
  			UniKitManager.request_notification_permission(UniKitManager.PUSH_PERMISSION_TYPE_SYSTEM_AND_SETTING, "push_guide")
  			GameState.inc_push_ask_count()



``

## PATH: scripts/module/setting/view/setting_page.gd (Partial)
``gdscript

  @onready var _sound_toggle_off: Panel = $Root / Content / PanelContainer / VBoxContainer / GridContainer / SoundCtrl 
/ SoundBtn / ToggleOff
  @onready var _vibration_toggle_on: Panel = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
VibrationCtrl / VibrationBtn / ToggleOn
  @onready var _vibration_toggle_off: Panel = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
VibrationCtrl / VibrationBtn / ToggleOff
  @onready var _icon_music: TextureRect = $Root / Content / PanelContainer / VBoxContainer / GridContainer / MusicCtrl 
/ MusicBtn / IconMusic
  @onready var _icon_sound: TextureRect = $Root / Content / PanelContainer / VBoxContainer / GridContainer / SoundCtrl 
/ SoundBtn / IconSound
  @onready var _icon_vibration: TextureRect = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
VibrationCtrl / VibrationBtn / IconVibration
  @onready var _people_toggle_on: Panel = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
PeopleCtrl / PeopleBtn / ToggleOn
  @onready var _people_toggle_off: Panel = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
PeopleCtrl / PeopleBtn / ToggleOff
  @onready var _icon_people: TextureRect = $Root / Content / PanelContainer / VBoxContainer / GridContainer / 
PeopleCtrl / PeopleBtn / IconPeople
  @onready var _terms_btn: UnderlineLink = $Root / Content / PanelContainer / VBoxContainer / TermContainer / TermsBtn
  @onready var _privacy_btn: UnderlineLink = $Root / Content / PanelContainer / VBoxContainer / TermContainer / 
PrivacyBtn
  @onready var _privacy_preference_btn: UnderlineLink = $Root / Content / PanelContainer / VBoxContainer / 
PrivacyContainer / PrivacyPreferenceBtn
  @onready var _version_label: Label = $Root / Content / PanelContainer / VBoxContainer / HBoxContainer / VersionLabel
  @onready var _panel_container: PanelContainer = $Root / Content / PanelContainer
  @onready var _anim: AnimationPlayer = $Root / AnimationPlayer
  
  
  @onready var _vb_how_to_play: Button = $Root / Content / PanelContainer / VBoxContainer / BtnContainer / HowToPlayBtn
  @onready var _vb_restart: Control = $Root / Content / PanelContainer / VBoxContainer / BtnContainer / 
OrangeRestartBtn
  @onready var _vb_restart_bg: Button = $Root / Content / PanelContainer / VBoxContainer / BtnContainer / 
OrangeRestartBtn / Bg
> @onready var _vb_feedback: Button = $Root / Content / PanelContainer / VBoxContainer / BtnContainer / FeedbackBtn
  @onready var _vb_language: Button = $Root / Content / PanelContainer / VBoxContainer / BtnContainer / LanguageBtn
  
  
  
  @onready var _language_switch_widget: LanguageSwitchWidget = $Root / Content / PanelContainer / VBoxContainer / 
ToggleContainer / LanguageSwitchWidget
  @onready var _vb_cmp_row: Control = $Root / Content / PanelContainer / VBoxContainer / PrivacyContainer
  @onready var _vb_term_row: Control = $Root / Content / PanelContainer / VBoxContainer / TermContainer
  @onready var _vb_version_row: Control = $Root / Content / PanelContainer / VBoxContainer / HBoxContainer
  
  @onready var _vb_sp3: Control = $Root / Content / PanelContainer / VBoxContainer / Control3
  @onready var _vb_sp6: Control = $Root / Content / PanelContainer / VBoxContainer / Control6
  
  @onready var _vb_pattern_sp: Control = $Root / Content / PanelContainer / VBoxContainer / Control4
  @onready var _vb_pattern_toggle: Control = $Root / Content / PanelContainer / VBoxContainer / ToggleContainer
  @onready var _pattern_switch: Control = $Root / Content / PanelContainer / VBoxContainer / ToggleContainer / 
PatternModeSwitch
  @onready var _pattern_on_panel: Panel = $Root / Content / PanelContainer / VBoxContainer / ToggleContainer / 
PatternModeSwitch / Content / Switch / On
  @onready var _pattern_off_panel: Panel = $Root / Content / PanelContainer / VBoxContainer / ToggleContainer / 
PatternModeSwitch / Content / Switch / Off
  @onready var _pattern_switch_hot: Control = $Root / Content / PanelContainer / VBoxContainer / ToggleContainer / 
PatternModeSwitch / Content / Switch
  
  var _sp3_static_miny: float = 0.0
  var _closing: bool = false
  
  var _restart_consumed: bool = false
  
  
  var _skip_next_close_anim: bool = false
  var _suppress_next_close_cb: bool = false
  
  func _ready() -> void :
  
  	_sp3_static_miny = _vb_sp3.custom_minimum_size.y
  	_update_toggle(_music_toggle_on, _music_toggle_off, _icon_music, GameState.is_music_on())
  	_update_toggle(_sound_toggle_on, _sound_toggle_off, _icon_sound, GameState.is_sound_on())
  	_update_toggle(_vibration_toggle_on, _vibration_toggle_off, _icon_vibration, GameState.is_vibration_on())
  	_update_toggle(_people_toggle_on, _people_toggle_off, _icon_people, GameState.is_people_on())
  	_refresh_dynamic_text()
  	bind_press_release_scale($Root / Content / PanelContainer / VBoxContainer / TitleBar / CloseBtn)
  
  	bind_press_release_scale(_vb_restart_bg)
  	bind_press_release_scale(_vb_how_to_play)
> 	bind_press_release_scale(_vb_feedback)
  	bind_press_release_scale(_vb_language)
  
  
  	claim_button_sound(_sound_btn)
  
  
  
  	_pattern_switch_hot.gui_input.connect(_on_pattern_switch_input)
  
  
  	for b in [_terms_btn, _privacy_preference_btn, _privacy_btn]:
  		b.resized.connect(_queue_unify_terms_row_font_size)
  
  	_language_switch_widget.language_picked.connect(_on_language_picked)
  
  
  
  	_language_switch_widget.dropdown_opened.connect(_on_language_dropdown_opened)
  	_language_switch_widget.dropdown_closed.connect(_on_language_dropdown_closed)
  
  
  
  func on_show(params: Dictionary = {}) -> void :
  
  
  	ABTestManager.dye_at_open_setting()
  	is_game_mode = params.get("is_game_mode", false)
  
> 	HelpshiftManager.request_unread()
  
  
  	_music_ctrl.visible = false
  
  
  	_people_ctrl.visible = _is_people_toggle_visible()
  
  
  
  
  	var _sys_locale: String = LanguageManager.resolve_system_locale()
  	var _sys_main: String = _sys_locale.split("_")[0]
  	var is_dropdown_mode: bool = ABTestManager.settings_language.is_dropdown_mode()
  	var show_language: bool = ( not is_game_mode) and ABTestManager.settings_language.is_language_switch_enabled()
  	if show_language and is_dropdown_mode and _sys_main == "en":
  		show_language = false
  	var show_widget: bool = show_language and is_dropdown_mode
  	var show_pattern: bool = is_game_mode and ABTestManager.blind_mod.is_enabled()
  	var show_toggle_container: bool = show_widget or show_pattern
  
  	var show_switch_dot: bool = show_pattern\
  	and GameState.is_tutorial_done()\
  	and not GameState.is_pattern_switch_dot_dismissed()
  	RedDotCenter.set_count(_DOT_PATTERN_SWITCH, 1 if show_switch_dot else 0)
  	_on_pattern_changed_cb = params.get("on_pattern_changed", Callable())
  
  	_apply_toggle_grid_layout()
  	_update_toggle(_music_toggle_on, _music_toggle_off, _icon_music, GameState.is_music_on())
  	_update_toggle(_people_toggle_on, _people_toggle_off, _icon_people, GameState.is_people_on())
  	_on_restart_cb = params.get("on_restart", Callable())
  	_on_close_cb = params.get("on_close", Callable())
  
  	var show_how_to_play: bool = is_game_mode and ABTestManager.rule_text.is_setting_entry()
  
  
  
  	var show_cmp: bool = ( not is_game_mode) and (UniKitManager.check_cmp_required() or debug_force_show_cmp)
  	apply_vbox_layout({
  		"show_how_to_play": show_how_to_play, 
  		"show_restart": is_game_mode, 
> 		"show_feedback": true, 
  		"show_language": show_language and not is_dropdown_mode, 
  		"show_cmp": show_cmp, 
  		"show_terms": not is_game_mode, 
  		"show_version": not is_game_mode, 
  	})
  
  	_set_miny(_vb_sp3, 0.0 if is_game_mode else _sp3_static_miny)
  
  
  	_set_miny(_vb_sp6, 90.0 if is_game_mode else 30.0)
  	_closing = false
  	_restart_consumed = false
  	_anim.play_section_with_markers("GenericPopup", &"", &"Mark")
  
  
  	_queue_unify_terms_row_font_size()
  
  
  	_center_panel_vertically()
  
  			continue
  		b.set_font_size_cap(min_fs)
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  func apply_vbox_layout(config: Dictionary) -> void :
  
  	_vb_how_to_play.visible = config.get("show_how_to_play", false)
  	_vb_restart.visible = config.get("show_restart", false)
> 	_vb_feedback.visible = config.get("show_feedback", true)
  	_vb_language.visible = config.get("show_language", false)
  	_vb_cmp_row.visible = config.get("show_cmp", false)
  	_vb_term_row.visible = config.get("show_terms", false)
  	_vb_version_row.visible = config.get("show_version", false)
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  func _apply_toggle_grid_layout() -> void :
  func _on_language_dropdown_opened() -> void :
  	Tracker.track_btn_click(Tracker.Btn.LANGUAGE, self)
  	Tracker.track_dlg_show(Tracker.Dlg.LANGUAGE_PICKER)
  
  
  
  
  func _on_language_dropdown_closed() -> void :
  	Tracker.notify_dlg_closed(Tracker.Dlg.LANGUAGE_PICKER)
  
  
  
  func _apply_language_and_close(locale: String) -> void :
  	Tracker.track_btn_click(Tracker.Btn.LANGUAGE_CONFIRM, self)
  	Tracker.track_user_property_ui_language(locale)
  	LanguageManager.set_locale(locale)
  	GameState.set_apply_locale(locale)
  	UIManager.hide_ui(get_ui_name())
  
  
> func _on_feedback_btn_pressed() -> void :
> 	Tracker.track_btn_click(Tracker.Btn.FEEDBACK, self)
  
  	if not UniKitManager.is_online():
  		Toast.popup("NETWORK_ERROR", self)
  		return
  
> 	HelpshiftManager.open_faq()
  
  
  
  
  
  func _is_people_toggle_visible() -> bool:
  	return true



``

## PATH: scripts/module/game_state/game_state.gd (Partial)
``gdscript

  var _endgame_store: SaveStore
  
  var _endgame_dirty: bool = false
  var _endgame_coalesce_timer: Timer
  
  
  
  
  signal tool_count_changed(kind: String, count: int)
  
  signal level_settled(won: bool)
  
  
  var _current_level: int = 1
  var _tutorial_done: bool = false
> var _has_shown_rate_us: bool = false
  
  var _has_used_revive_free: bool = false
  
  var _warn_life_shown: bool = false
  
  
  var _life_plus_first_done: bool = false
  
  var _current_strategy: int = 1
  
  
  
  var _consecutive_clean_wins: int = 0
  
  
  
  func get_current_level() -> int:
  	return _current_level
  
  func set_current_level(value: int) -> void :
  	_current_level = value
  	_save_data()
  
  func is_tutorial_done() -> bool:
  	return _tutorial_done
  
  func set_tutorial_done(value: bool) -> void :
  	_tutorial_done = value
  	_save_data()
  
> func has_shown_rate_us() -> bool:
> 	return _has_shown_rate_us
  
  func has_used_revive_free() -> bool:
  	return _has_used_revive_free
  
  func mark_revive_free_used() -> void :
  	if _has_used_revive_free:
  		return
  	_has_used_revive_free = true
  	_save_data()
  
  
  func has_won_since_cold_start() -> bool:
  	return _has_won_since_cold_start
  
  
  
  func increment_session_reward_view_count() -> void :
  	_session_reward_view_count += 1
  
  
  
  func reset_session_reward_view_count() -> void :
  	_session_reward_view_count = 0
  
  
  func set_session_reward_view_count(value: int) -> void :
  	_session_reward_view_count = max(0, value)
  
  
  func mark_rate_us_shown() -> void :
> 	if _has_shown_rate_us:
  		return
> 	_has_shown_rate_us = true
  	_save_data()
  
  
  func reset_rate_us_shown() -> void :
> 	if not _has_shown_rate_us:
  		return
> 	_has_shown_rate_us = false
  	_save_data()
  
  func has_shown_warn_life() -> bool:
  	return _warn_life_shown
  
  
  func mark_warn_life_shown() -> void :
  	if _warn_life_shown:
  		return
  	_warn_life_shown = true
  	_save_data()
  
  
  func reset_warn_life_shown() -> void :
  	if not _warn_life_shown:
  		tool_count_changed.emit("hint", _tool_hint)
  
  func _save_data() -> void :
  	var cfg: = ConfigFile.new()
  	cfg.set_value("progress", "current_level", _current_level)
  	cfg.set_value("progress", "tutorial_done", _tutorial_done)
  	cfg.set_value("progress", "current_strategy", _current_strategy)
  	cfg.set_value("progress", "consecutive_clean_wins", _consecutive_clean_wins)
  	cfg.set_value("progress", "last_level_clean_win", _last_level_clean_win)
  	cfg.set_value("progress", "consecutive_fails", _consecutive_fails)
  	cfg.set_value("progress", "consecutive_retry_levels", _consecutive_retry_levels)
  	cfg.set_value("progress", "retry_tracking_strategy", _retry_tracking_strategy)
  	cfg.set_value("progress", "bank_progress", _bank_progress)
  	cfg.set_value("progress", "main_bank_progress", _main_bank_progress)
  	cfg.set_value("progress", "lkmod_progress", _lkmod_progress)
> 	cfg.set_value("progress", "has_shown_rate_us", _has_shown_rate_us)
  	cfg.set_value("progress", "has_used_revive_free", _has_used_revive_free)
  	cfg.set_value("progress", "warn_life_shown", _warn_life_shown)
  	cfg.set_value("progress", "life_plus_first_done", _life_plus_first_done)
  	cfg.set_value("progress", "daily_index", _daily_index)
  	cfg.set_value("progress", "daily_completed_date", _daily_completed_date)
  	cfg.set_value("progress", "max_daily_date", _max_daily_date)
  	cfg.set_value("progress", "daily_elapsed_sec", _daily_elapsed_sec)
  	cfg.set_value("progress", "daily_beat_percent", _daily_beat_percent)
  	cfg.set_value("progress", "daily_best_beat_percent", _daily_best_beat_percent)
  	cfg.set_value("progress", "daily_started_date", _daily_started_date)
  	cfg.set_value("progress", "daily_first_easy_date", _daily_first_easy_date)
  	cfg.set_value("progress", "game_total_stats", _game_total_stats)
  
  	cfg.set_value("progress", "main_game_total_stats", {})
  	cfg.set_value("progress", "daily_game_total_stats", {})
  	var cfg: = _player_store.load_config()
  	if cfg == null:
  		_resolve_endgame_store()
  		return
  	_current_level = cfg.get_value("progress", "current_level", 1)
  	_tutorial_done = cfg.get_value("progress", "tutorial_done", false)
  	_current_strategy = cfg.get_value("progress", "current_strategy", 1)
  	_consecutive_clean_wins = cfg.get_value("progress", "consecutive_clean_wins", 0)
  	_last_level_clean_win = cfg.get_value("progress", "last_level_clean_win", false)
  	_consecutive_fails = cfg.get_value("progress", "consecutive_fails", 0)
  	_consecutive_retry_levels = cfg.get_value("progress", "consecutive_retry_levels", 0)
  	_retry_tracking_strategy = cfg.get_value("progress", "retry_tracking_strategy", 0)
  	_bank_progress = cfg.get_value("progress", "bank_progress", {})
  	_main_bank_progress = cfg.get_value("progress", "main_bank_progress", {})
  	_lkmod_progress = cfg.get_value("progress", "lkmod_progress", {})
> 	_has_shown_rate_us = cfg.get_value("progress", "has_shown_rate_us", false)
  	_has_used_revive_free = cfg.get_value("progress", "has_used_revive_free", false)
  	_warn_life_shown = cfg.get_value("progress", "warn_life_shown", false)
  	_life_plus_first_done = cfg.get_value("progress", "life_plus_first_done", false)
  	_daily_index = cfg.get_value("progress", "daily_index", 0)
  	_daily_completed_date = cfg.get_value("progress", "daily_completed_date", "")
  	_max_daily_date = cfg.get_value("progress", "max_daily_date", "")
  	_daily_elapsed_sec = cfg.get_value("progress", "daily_elapsed_sec", 0)
  	_daily_beat_percent = cfg.get_value("progress", "daily_beat_percent", 0.0)
  	_daily_best_beat_percent = cfg.get_value("progress", "daily_best_beat_percent", 0.0)
  	_daily_started_date = cfg.get_value("progress", "daily_started_date", "")
  	_daily_first_easy_date = cfg.get_value("progress", "daily_first_easy_date", "")
  	_game_total_stats = cfg.get_value("progress", "game_total_stats", {})
  	_main_game_total_stats = cfg.get_value("progress", "main_game_total_stats", {})
  	_daily_game_total_stats = cfg.get_value("progress", "daily_game_total_stats", {})
  	_main_game_round_stats = cfg.get_value("progress", "main_game_round_stats", {})
  
  func _today_str() -> String:
  	var dt: Dictionary = Time.get_date_dict_from_system()
  	return "%d-%02d-%02d" % [dt.year, dt.month, dt.day]
  
  
  
  func _inc_today_win_count() -> void :
  	var today: String = _today_str()
  	_recent_win_counts_by_day[today] = _recent_win_counts_by_day.get(today, 0) + 1
  
  
  func reset_all() -> void :
  	_current_level = 1
  	_tutorial_done = false
> 	_has_shown_rate_us = false
  	_has_used_revive_free = false
  	_warn_life_shown = false
  	_life_plus_first_done = false
  	_current_strategy = 1
  	_consecutive_clean_wins = 0
  	_last_level_clean_win = false
  	_consecutive_fails = 0
  	_consecutive_retry_levels = 0
  	_retry_tracking_strategy = 0
  	_current_level_dirty = false
  	_current_level_retried = false
  	_retry_puzzle_level = 0
  	_retry_puzzle_params = {}
  	_pre_cat_fail_lv = 0
  	_pre_cat_fail_count = 0



``

# PROJECT: UNITY

## PATH: Assets/_Project/Scripts/Gameplay/GameWinPagePresenter.cs
``csharp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DG.Tweening;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameWinPagePresenter : UIFrameWindow,
        IDailyMetaConsumer,
        IProfileConsumer,
        IRankActivityConsumer,
        IPlatformPermissionRuntimeConsumer
    {
        public override string GetTrackingScreenName() =>
            _selfName == UiName.DailyWin
                ? TrackerCatalog.Screen.DailyWin
                : TrackerCatalog.Screen.NormalWin;

        private static readonly string[] NormalTitleKeys =
        {
            "WIN_TITLE", "WIN_TITLE_1", "WIN_TITLE_2", "WIN_TITLE_3",
            "WIN_TITLE_4"
        };

        [SerializeField] private RectTransform content;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private GameObject defaultVisuals;
        [SerializeField] private RectTransform rayLight;
        [SerializeField] private RectTransform victoryCat;
        [SerializeField] private Text titleText;
        [SerializeField] private GameObject bodyRoot;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject statisticsRoot;
        [SerializeField] private CanvasGroup statisticsGroup;
        [SerializeField] private Text timeText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text nextButtonText;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject passPanelRoot;
        [SerializeField] private RectTransform passPanelPopup;
        [SerializeField] private CanvasGroup passPanelGroup;
        [SerializeField] private Text passTitleText;
        [SerializeField] private Text passPraiseText;
        [SerializeField] private RectTransform passPraiseRect;
        [SerializeField] private RectTransform passStatsRoot;
        [SerializeField] private RectTransform passActionsRect;
        [SerializeField] private Text passSizeKeyText;
        [SerializeField] private Text passTimeKeyText;
        [SerializeField] private Text passScoreKeyText;
        [SerializeField] private Text passComboKeyText;
        [SerializeField] private Text passSizeText;
        [SerializeField] private Text passTimeText;
        [SerializeField] private Text passScoreText;
        [SerializeField] private Text passComboText;
        [SerializeField] private GameObject passExtraRoot;
        [SerializeField] private Text passCompletionText;
        [SerializeField] private Text passMistakeText;
        [SerializeField] private Text passToolsText;
        [SerializeField] private Text passNextButtonText;
        [SerializeField] private Button passNextButton;
        [Header("Daily result presentation")]
        [SerializeField] private GameObject dailyVisuals;
        [SerializeField] private RectTransform dailyContent;
        [SerializeField] private CanvasGroup dailyContentGroup;
        [SerializeField] private RectTransform dailyRayLight;
        [SerializeField] private RectTransform dailyVictoryCat;
        [SerializeField] private Text dailyTitleText;
        [SerializeField] private Text dailyTimeText;
        [SerializeField] private Text dailyBeatText;
        [SerializeField] private Text dailyContinueText;
        [SerializeField] private Button dailyContinueButton;
        [SerializeField] private LocalizationCatalog localization;

        private readonly PassPageConfig _passPageConfig = new();
        private readonly PassTextConfig _passTextConfig = new();
        private GameplayManager _gameplayManager;
        private Sequence _openTween;
        private Tween _rayTween;
        private Tween _catTween;
        private Sequence _statisticsTween;
        private Tween _passNextReadyTween;
        private string _lastNormalTitleKey = string.Empty;
        private MainGameTransitionData _transition;
        private UiName _selfName = UiName.Win;
        private DailyMetaRuntime _dailyMetaRuntime;
        private ProfileRuntime _profileRuntime;
        private RankActivityRuntime _rankActivityRuntime;
        private bool _continuing;
        private PrivacyPermissionRuntime _platformRuntime;
        private int _pushFlowGeneration;
        private const float PushGuideAppearDelaySeconds = 2.467f;

        protected override void OnCreate()
        {
            if (nextButton != null) nextButton.onClick.AddListener(Continue);
            if (passNextButton != null)
                passNextButton.onClick.AddListener(Continue);
            if (dailyContinueButton != null)
                dailyContinueButton.onClick.AddListener(Continue);
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            MainGameTransitionData transition = ReadTransition(parameters);
            _transition = transition;
            _gameplayManager = ReadManager(parameters);
            _continuing = false;
            _pushFlowGeneration++;
            if (transition == null)
            {
                Owner?.Hide(UiName.Win);
                Owner?.Hide(UiName.DailyWin);
                return;
            }
            _selfName = transition.IsDailySession
                ? UiName.DailyWin
                : UiName.Win;

            bool passPanel = RefreshText(transition);
            if (transition.IsDailySession)
            {
                if (dailyContinueButton != null)
                    dailyContinueButton.interactable = true;
                Owner?.BlockInputBriefly(
                    transform as RectTransform,
                    DailyResultContract.InputBlockSeconds);
                bool toastWasShown = ReadBool(
                    parameters,
                    "toast_was_shown");
                if (toastWasShown)
                    StartDailyPresentation();
                else
                    StartManagedCoroutine(StartDailyPresentationAfterDelay());
                return;
            }
            if (passPanel)
            {
                PlayPassPanelAnimation();
                _gameplayManager?.PlayResultSound(SoundKind.PassPageSettle);
            }
            else
            {
                PlayOpenAnimation();
                _gameplayManager?.PlayResultSound(SoundKind.LevelWin);
            }
            _platformRuntime?.PrepareNormalGameEnd(transition.Level);
            if (_platformRuntime?.IsPushGuideEligible(transition.Level) == true)
            {
                Owner?.BlockInputBriefly(
                    transform as RectTransform,
                    PushGuideAppearDelaySeconds);
                StartManagedCoroutine(ShowPushGuideAfterAppear(
                    _pushFlowGeneration,
                    transition.Level));
            }
        }

        protected override IEnumerator OnHide()
        {
            KillTweens();
            _gameplayManager = null;
            _transition = null;
            _continuing = false;
            _pushFlowGeneration++;
            yield break;
        }

        protected override bool OnBackRequest() => true;

        protected override void OnDestroyWindow()
        {
            KillTweens();
            if (nextButton != null) nextButton.onClick.RemoveListener(Continue);
            if (passNextButton != null)
                passNextButton.onClick.RemoveListener(Continue);
            if (dailyContinueButton != null)
                dailyContinueButton.onClick.RemoveListener(Continue);
            base.OnDestroyWindow();
        }

        public void BindDailyMetaRuntime(DailyMetaRuntime runtime)
        {
            _dailyMetaRuntime = runtime;
        }

        public void BindProfileRuntime(ProfileRuntime runtime)
        {
            _profileRuntime = runtime;
        }

        public void BindRankActivityRuntime(RankActivityRuntime runtime)
        {
            _rankActivityRuntime = runtime;
        }

        public void BindPlatformPermissionRuntime(
            PrivacyPermissionRuntime runtime)
        {
            _platformRuntime = runtime;
        }

        private IEnumerator ShowPushGuideAfterAppear(
            int generation,
            int level)
        {
            yield return new WaitForSecondsRealtime(
                PushGuideAppearDelaySeconds);
            if (generation != _pushFlowGeneration || !IsShowing ||
                _platformRuntime == null)
                yield break;
            yield return _platformRuntime.TryShowPushGuide(level);
        }

        private bool RefreshText(MainGameTransitionData transition)
        {
            if (transition.IsDailySession)
                return RefreshDailyText(transition);

            PassTextStrategySelection selection =
                PassTextStrategyContract.Select(
                    _passTextConfig.Value,
                    new PassTextStrategyInput
                    {
                        Level = transition.Level,
                        Size = transition.Size,
                        RestartCount = transition.RestartCount,
                        ReviveCount = transition.ReviveCount,
                        MistakeCount = transition.MistakeCount,
                        ElapsedSeconds = transition.ElapsedSeconds,
                        LastWinBeatPercent =
                            GameStateRuntime.Current.LastWinBeatPercent,
                        IsHard = transition.Level > 0 &&
                                 LevelData.IsHardLevel(transition.Level)
                    },
                    UnityEngine.Random.Range(0, int.MaxValue),
                    UnityEngine.Random.value);
            if (dailyVisuals != null) dailyVisuals.SetActive(false);
            GameStateRuntime.Current.SetLastWinBeatPercent(
                (float)selection.ShownPercent);

            string titleKey = !string.IsNullOrEmpty(selection.TitleKey)
                ? selection.TitleKey
                : LevelData.IsHardLevel(transition.Level)
                    ? "WIN_TITLE_HARD"
                    : PickNormalTitleKey();
            string title = Translate(titleKey, "Great!");
            string body = !string.IsNullOrEmpty(selection.BodyKey)
                ? StrategyBody(selection)
                : string.Empty;
            bool showPassPanel = _passPageConfig.IsG1() ||
                                 _passPageConfig.IsG2();
            if (defaultVisuals != null) defaultVisuals.SetActive(!showPassPanel);
            if (content != null) content.gameObject.SetActive(!showPassPanel);
            if (passPanelRoot != null) passPanelRoot.SetActive(showPassPanel);
            if (showPassPanel)
            {
                PopulatePassPanel(transition, title, body);
                return true;
            }

            SetText(titleText, title);

            bool showBody = !string.IsNullOrEmpty(selection.BodyKey);
            if (bodyRoot != null) bodyRoot.SetActive(showBody);
            if (showBody)
                SetText(bodyText, body);

            bool showStatistics = _passPageConfig.IsG4();
            if (statisticsRoot != null) statisticsRoot.SetActive(showStatistics);
            if (showStatistics)
            {
                if (statisticsGroup != null) statisticsGroup.alpha = 0f;
                SetText(timeText,
                    $"{Translate("PASS_PAGE_TIME", "Time")}  {FormatTime(0f)}");
                SetText(scoreText,
                    $"{Translate("PASS_PAGE_SCORE", "Score")}  {FormatScore(0)}");
                SetText(comboText,
                    $"{Translate("PASS_PAGE_COMBO", "Combo")}  0");
            }

            string nextLabel = transition.IsBankSession
                ? transition.NextBankLabel
                : Translate("GAME_LEVEL_TITLE", "Level %d").Replace(
                    "%d",
                    transition.CurrentLevelAfter.ToString());
            SetText(nextButtonText, nextLabel);
            if (nextButton != null)
                nextButton.interactable = !string.IsNullOrEmpty(nextLabel);
            return false;
        }

        private bool RefreshDailyText(MainGameTransitionData transition)
        {
            if (defaultVisuals != null) defaultVisuals.SetActive(false);
            if (content != null) content.gameObject.SetActive(false);
            if (passPanelRoot != null) passPanelRoot.SetActive(false);
            if (dailyVisuals != null) dailyVisuals.SetActive(true);

            SetText(
                dailyTitleText,
                Translate("DAILY_WIN_TITLE", "Challenge Cleared"));
            string time = DailyResultContract.FormatElapsedSeconds(
                transition.ElapsedSeconds);
            string percent = DailyResultContract.FormatBeatPercent(
                transition.DailyBeatPercent);
            SetText(
                dailyTimeText,
                "<color=#FFF1B9>" +
                Translate("DAILY_WIN_TIME", "Time") +
                " </color><color=#F19320><size=80><b>" + time +
                "</b></size></color>");
            string highlighted =
                "</color><color=#02BE52><size=90><b>" + percent +
                "</b></size></color><color=#FFE375>";
            string beat = Translate(
                    "DAILY_WIN_BEAT",
                    "Beat %s of players!")
                .Replace("%s", highlighted);
            SetText(dailyBeatText, "<color=#FFE375>" + beat + "</color>");
            SetText(
                dailyContinueText,
                Translate("WIN_CONTINUE", "Continue"));
            return false;
        }

        private IEnumerator StartDailyPresentationAfterDelay()
        {
            yield return new WaitForSecondsRealtime(
                DailyResultContract.AppearDelaySeconds);
            if (_transition != null && _transition.IsDailySession)
                StartDailyPresentation();
        }

        private void StartDailyPresentation()
        {
            PlayDailyOpenAnimation();
            _gameplayManager?.PlayResultSound(SoundKind.LevelWin);
        }

        private void PopulatePassPanel(
            MainGameTransitionData transition,
            string title,
            string praise)
        {
            SetText(passTitleText, title);
            SetText(passPraiseText, PassPraise(praise));
            if (passPraiseText != null)
                passPraiseText.gameObject.SetActive(!string.IsNullOrEmpty(praise));
            SetText(passSizeKeyText, Translate("PASS_PAGE_SIZE", "Size"));
            SetText(passTimeKeyText, Translate("PASS_PAGE_TIME", "Time"));
            SetText(passScoreKeyText, Translate("PASS_PAGE_SCORE", "Score"));
            SetText(passComboKeyText, Translate("PASS_PAGE_COMBO", "Combo"));
            SetText(passSizeText, $"{transition.Size}\u00D7{transition.Size}");
            SetText(passTimeText, FormatTime(transition.ElapsedSeconds));
            SetText(passScoreText, FormatScore(transition.FinalScore));
            SetText(passComboText, transition.MaxCombo.ToString());

            bool showExtra = _passPageConfig.IsG2();
            ApplyPassLayout(showExtra);
            if (passExtraRoot != null) passExtraRoot.SetActive(showExtra);
            if (showExtra)
            {
                SetText(passCompletionText, transition.CompletionRate + "%");
                SetText(passMistakeText, transition.MistakeCount.ToString());
                SetText(passToolsText, transition.ToolsUsed.ToString());
            }

            string nextLabel = transition.IsBankSession
                ? transition.NextBankLabel
                : Translate("GAME_LEVEL_TITLE", "Level %d").Replace(
                    "%d",
                    transition.CurrentLevelAfter.ToString());
            SetText(passNextButtonText, nextLabel);
            if (passNextButton != null)
                passNextButton.interactable = false;
        }

        private void ApplyPassLayout(bool group2)
        {
            if (passPanelPopup != null)
            {
                passPanelPopup.anchoredPosition = new Vector2(
                    0f,
                    group2 ? 366f : 372f);
                passPanelPopup.sizeDelta = new Vector2(
                    900f,
                    group2 ? 1072f : 912f);
            }
            if (passTitleText != null)
                passTitleText.rectTransform.anchoredPosition = new Vector2(
                    0f,
                    group2 ? 260f : 172f);
            if (passStatsRoot != null)
                passStatsRoot.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -62f : -150f);
            if (passPraiseRect != null)
                passPraiseRect.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -360f : -274f);
            if (passActionsRect != null)
                passActionsRect.anchoredPosition = new Vector2(
                    0f,
                    group2 ? -630f : -544f);
        }

        private void PlayPassPanelAnimation()
        {
            KillTweens();
            if (passPanelPopup != null)
                passPanelPopup.localScale = Vector3.one * 0.5f;
            if (passPanelGroup != null) passPanelGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (passPanelPopup != null)
                _openTween.Append(
                        passPanelPopup.DOScale(1.1f, 0.2f)
                            .SetEase(Ease.OutQuad))
                    .Append(
                        passPanelPopup.DOScale(1f, 0.133333f)
                            .SetEase(Ease.InOutQuad));
            if (passPanelGroup != null)
                _openTween.Insert(0f, passPanelGroup.DOFade(1f, 0.2f));
            _openTween.OnComplete(() => _openTween = null);
            _passNextReadyTween = DOVirtual.DelayedCall(
                    0.69804f,
                    () =>
                    {
                        if (passNextButton != null)
                            passNextButton.interactable = true;
                        _passNextReadyTween = null;
                    })
                .SetLink(gameObject);
        }

        private void PlayOpenAnimation()
        {
            KillTweens();
            if (content != null) content.localScale = Vector3.one * 0.7f;
            if (contentGroup != null) contentGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (content != null)
                _openTween.Append(content.DOScale(1.05f, 0.18f).SetEase(Ease.OutQuad))
                    .Append(content.DOScale(1f, 0.08f).SetEase(Ease.InOutQuad));
            if (contentGroup != null)
                _openTween.Insert(0f, contentGroup.DOFade(1f, 0.18f));
            _openTween.OnComplete(() => _openTween = null);

            if (rayLight != null)
                _rayTween = rayLight.DORotate(
                        new Vector3(0f, 0f, -360f),
                        12f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1)
                    .SetLink(gameObject);
            if (victoryCat != null)
                _catTween = victoryCat.DOScale(1.035f, 0.65f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            PlayStatisticsRoll();
        }

        private void PlayDailyOpenAnimation()
        {
            KillTweens();
            if (dailyContent != null)
                dailyContent.localScale = Vector3.one * 0.7f;
            if (dailyContentGroup != null)
                dailyContentGroup.alpha = 0f;
            _openTween = DOTween.Sequence().SetLink(gameObject);
            if (dailyContent != null)
                _openTween.Append(
                        dailyContent.DOScale(1.05f, 0.18f)
                            .SetEase(Ease.OutQuad))
                    .Append(
                        dailyContent.DOScale(1f, 0.08f)
                            .SetEase(Ease.InOutQuad));
            if (dailyContentGroup != null)
                _openTween.Insert(
                    0f,
                    dailyContentGroup.DOFade(1f, 0.18f));
            _openTween.OnComplete(() => _openTween = null);

            if (dailyRayLight != null)
                _rayTween = dailyRayLight.DORotate(
                        new Vector3(0f, 0f, -360f),
                        12f,
                        RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetLoops(-1)
                    .SetLink(gameObject);
            if (dailyVictoryCat != null)
                _catTween = dailyVictoryCat.DOScale(1.035f, 0.65f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
        }

        private void PlayStatisticsRoll()
        {
            if (!_passPageConfig.IsG4() || _transition == null) return;
            string timeKey = Translate("PASS_PAGE_TIME", "Time");
            string scoreKey = Translate("PASS_PAGE_SCORE", "Score");
            string comboKey = Translate("PASS_PAGE_COMBO", "Combo");
            int elapsed = Mathf.CeilToInt(_transition.ElapsedSeconds);
            int score = _transition.FinalScore;
            int combo = _transition.MaxCombo;
            _statisticsTween = DOTween.Sequence().SetLink(gameObject);
            if (statisticsGroup != null)
                _statisticsTween.Insert(
                    0.8f,
                    statisticsGroup.DOFade(1f, 0.2f));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, elapsed, 0.65f, value =>
                    SetText(timeText, $"{timeKey}  {FormatTime(value)}")));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, score, 0.65f, value =>
                    SetText(scoreText, $"{scoreKey}  {FormatScore(value)}")));
            _statisticsTween.Insert(
                1f,
                DOVirtual.Int(0, combo, 0.65f, value =>
                    SetText(comboText, $"{comboKey}  {value}")));
            _statisticsTween.OnComplete(() => _statisticsTween = null);
        }

        private void Continue()
        {
            if (_continuing) return;
            Tracking?.TrackButtonClick(
                _transition?.IsDailySession == true
                    ? TrackerCatalog.Button.Continue
                    : TrackerCatalog.Button.LevelPlay,
                GetTrackingScreenName());
            _continuing = true;
            SetContinueInteractable(false);
            StartManagedCoroutine(ContinueAfterMetaFlows());
        }

        private IEnumerator ContinueAfterMetaFlows()
        {
            bool main = _transition != null &&
                        !_transition.IsDailySession &&
                        !_transition.IsBankSession &&
                        _transition.Level > 0;
            RankActivityManager rank = main
                ? _rankActivityRuntime?.Manager
                : null;
            if (rank?.GetPendingReward() != null)
            {
                int uid = rank.ClaimReward(false);
                if (uid >= 0 && Owner != null)
                {
                    yield return null;
                    yield return Owner.AwaitHidden(UiName.Award);
                }
                if (!IsShowing) yield break;
            }
            rank?.MaybeOpen(false);

            if (_dailyMetaRuntime != null &&
                _dailyMetaRuntime.Streak.IsSettleReorder &&
                StreakFlowCoordinator.HasPendingFlow(_dailyMetaRuntime))
            {
                yield return StreakFlowCoordinator.RunAfterResult(
                    Owner,
                    _dailyMetaRuntime);
            }
            if (!IsShowing) yield break;

            if (rank?.IsOpenNotJoined == true && Owner != null)
            {
                var popup = Owner.Show(UiName.RankActivityOpenPopup) as
                    RankActivityOpenPopupPresenter;
                if (popup != null)
                {
                    yield return Owner.AwaitHidden(
                        UiName.RankActivityOpenPopup);
                    rank.ConfirmParticipation();
                    if (rank.PeriodCount == 1 &&
                        _profileRuntime?.Service?.IsIdentityDefault == true)
                    {
                        UIFrameWindow profile = Owner.Show(
                            UiName.Profile,
                            new Dictionary<string, object>(1)
                            {
                                ["from_rank_open_guide"] = true
                            });
                        if (profile != null)
                            yield return Owner.AwaitHidden(UiName.Profile);
                        if (!IsShowing) yield break;
                    }
                }
            }
            FinishContinue();
        }

        private void FinishContinue()
        {
            if (_transition?.IsDailySession == true)
            {
                if (Owner == null || _gameplayManager == null)
                {
                    _continuing = false;
                    SetContinueInteractable(true);
                    return;
                }

                UIFrameWindow mainGame = Owner.Show(
                    UiName.Game,
                    new Dictionary<string, object>(2)
                    {
                        ["level_index"] = GameStateRuntime.Current.CurrentLevel,
                        ["_tracker_status"] = TrackerCatalog.GameStatus.Continue
                    });
                if (mainGame == null)
                {
                    _continuing = false;
                    SetContinueInteractable(true);
                    return;
                }

                Owner.Hide(UiName.DailyWin);
                Owner.Hide(UiName.DailyGame);
                return;
            }

            if (_gameplayManager == null || !_gameplayManager.ContinueToNextLevel())
            {
                _continuing = false;
                SetContinueInteractable(true);
                return;
            }
            Owner?.Hide(_selfName);
        }

        private void SetContinueInteractable(bool interactable)
        {
            if (nextButton != null)
                nextButton.interactable = interactable;
            if (passNextButton != null)
                passNextButton.interactable = interactable;
            if (dailyContinueButton != null)
                dailyContinueButton.interactable = interactable;
        }

        private void KillTweens()
        {
            _openTween?.Kill(false);
            _rayTween?.Kill(false);
            _catTween?.Kill(false);
            _statisticsTween?.Kill(false);
            _passNextReadyTween?.Kill(false);
            _openTween = null;
            _rayTween = null;
            _catTween = null;
            _statisticsTween = null;
            _passNextReadyTween = null;
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string translated = localization.Translate(key);
            return translated == key ? fallback : translated;
        }

        private string StrategyBody(PassTextStrategySelection selection)
        {
            string template = Translate(selection.BodyKey, selection.BodyKey);
            string percent = HighlightPercent(selection.Percent);
            string difference = HighlightPercent(selection.DifferencePercent);
            return template
                .Replace("{pct}", percent)
                .Replace("{diff}", difference)
                .Replace("{br}", "\n")
                .Replace("[center]", string.Empty)
                .Replace("[/center]", string.Empty);
        }

        private static string HighlightPercent(double value)
        {
            if (value < 0.0) return string.Empty;
            string text = value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
            return $"<size=90><b><color=#02BE52>{text}</color></b></size>";
        }

        private static string PassPraise(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string result = value.Replace("#02BE52", "#F19320");
            int leading = 0;
            while (leading < result.Length && char.IsDigit(result[leading]))
                leading++;
            if (leading > 0)
                result = "<color=#F19320>" + result.Substring(0, leading) +
                         "</color>" + result.Substring(leading);
            if (!result.Contains("<color="))
                result = Regex.Replace(
                    result,
                    @"\d+(?:\.\d+)?%",
                    "<color=#F19320>$0</color>");
            return result;
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Clamp(
                Mathf.CeilToInt(seconds),
                0,
                24 * 60 * 60 - 1);
            int hours = total / 3600;
            return hours > 0
                ? $"{hours:00}:{total % 3600 / 60:00}:{total % 60:00}"
                : $"{total / 60:00}:{total % 60:00}";
        }

        private string PickNormalTitleKey()
        {
            int index = UnityEngine.Random.Range(0, NormalTitleKeys.Length);
            if (NormalTitleKeys[index] == _lastNormalTitleKey)
            {
                int offset = UnityEngine.Random.Range(
                    1,
                    NormalTitleKeys.Length);
                index = (index + offset) % NormalTitleKeys.Length;
            }
            _lastNormalTitleKey = NormalTitleKeys[index];
            return _lastNormalTitleKey;
        }

        private static string FormatScore(int value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value ?? string.Empty;
        }

        private static MainGameTransitionData ReadTransition(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue("transition", out object value)
                ? value as MainGameTransitionData
                : null;
        }

        private static GameplayManager ReadManager(
            IReadOnlyDictionary<string, object> parameters)
        {
            return parameters != null &&
                   parameters.TryGetValue("gameplay_manager", out object value)
                ? value as GameplayManager
                : null;
        }

        private static bool ReadBool(
            IReadOnlyDictionary<string, object> parameters,
            string key)
        {
            if (parameters == null ||
                !parameters.TryGetValue(key, out object value) ||
                value == null)
                return false;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/GameStateData.cs
``csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core
{
    /// <summary>
    /// Typed P0 persistence slice ported from game_state.gd.
    /// Serialized keys intentionally retain the source snake_case names.
    /// </summary>
    public sealed class GameStateData
    {
        public int CurrentLevel { get; set; } = 1;
        public bool IsFirstSession { get; set; } = true;
        public bool TutorialDone { get; set; }
        public int CurrentStrategy { get; set; } = 1;
        public int ConsecutiveCleanWins { get; set; }
        public bool LastLevelCleanWin { get; set; }
        public int ConsecutiveFails { get; set; }
        public int ConsecutiveRetryLevels { get; set; }
        public int RetryTrackingStrategy { get; set; }
        public int DailyIndex { get; set; }
        public string DailyCompletedDate { get; set; } = string.Empty;
        public string MaxDailyDate { get; set; } = string.Empty;
        public int DailyElapsedSeconds { get; set; }
        public float DailyBeatPercent { get; set; }
        public float DailyBestBeatPercent { get; set; }
        public string DailyStartedDate { get; set; } = string.Empty;
        public string DailyFirstEasyDate { get; set; } = string.Empty;
        public Dictionary<string, object> RecentWinCountsByDay { get; set; } =
            new Dictionary<string, object>();
        public int SessionCount { get; set; }
        public int TodaySessionCount { get; set; }
        public int LastDaySessionCount { get; set; }
        public int ActiveDays { get; set; }
        public int TodayPlayedCount { get; set; }
        public int TodayActiveSeconds { get; set; }
        public int TotalActiveSeconds { get; set; }
        public List<int> GrtLevelD90Reported { get; set; } = new();
        public List<string> GrtReportedEvents { get; set; } = new();
        public long FirstOpenTimeMs { get; set; }
        public string TodayDate { get; set; } = string.Empty;

        public Dictionary<string, object> BankProgress { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainBankProgress { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> LkModifiedProgress { get; set; } =
            new Dictionary<string, object>();

        public int ToolLocate { get; set; } = 5;
        public int ToolHint { get; set; } = 5;
        public int ToolUndo { get; set; } = 3;
        public string LastSplashDate { get; set; } = string.Empty;
        public bool HasUsedTool { get; set; }
        public bool PropHighlightShown { get; set; }
        public int PushAskCount { get; set; }
        public string PushGuideLastDate { get; set; } = string.Empty;
        public int PushGuideShownCount { get; set; }
        public int PushGuidePopupCount { get; set; }
        public bool HasShownAttGuide { get; set; }
        public string AppliedLocale { get; set; } = string.Empty;
        public bool MusicOn { get; set; } = true;
        public bool MusicUserModified { get; set; }
        public bool SoundOn { get; set; } = true;
        public bool VibrationOn { get; set; } = true;
        public bool PeopleOn { get; set; } = true;
        public bool PatternModeOn { get; set; }
        public bool PatternEntryDotDismissed { get; set; }
        public bool PatternSwitchDotDismissed { get; set; }
        public bool HasUsedReviveFree { get; set; }
        public bool InterstitialUnlocked { get; set; }
        public bool BannerUnlocked { get; set; }
        public float LastWinBeatPercent { get; set; } = -1f;

        public int RetryPuzzleLevel { get; set; }
        public Dictionary<string, object> RetryPuzzleParameters { get; set; } =
            new Dictionary<string, object>();

        public int PreCatFailLevel { get; set; }
        public int PreCatFailCount { get; set; }
        public bool PreCatRevivedThisLevel { get; set; }
        public bool PreCatPendingHard { get; set; }
        public bool PreCatPendingStruggle { get; set; }
        public bool PreCatPendingDemote { get; set; }
        public int PreCatLockLevel { get; set; }
        public string PreCatLockType { get; set; } = "0";
        public Vector2Int PreCatLockPosition { get; set; } = new Vector2Int(-1, -1);

        public List<object> RecentPuzzles { get; set; } = new List<object>();
        public List<object> InFlightAwards { get; set; } = new List<object>();
        public List<object> PendingRewards { get; set; } = new List<object>();
        public List<object> RewardHistoryTimestamps { get; set; } =
            new List<object>();
        public int RestoredTodayCount { get; set; }
        public int SavedGameAutoMark { get; set; } = -1;
        public Dictionary<string, object> SavedAbGroups { get; set; } =
            new Dictionary<string, object>();

        public Dictionary<string, object> EndgameSnapshot { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainGameTotalStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> DailyGameTotalStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> MainGameRoundStats { get; set; } =
            new Dictionary<string, object>();
        public Dictionary<string, object> DailyGameRoundStats { get; set; } =
            new Dictionary<string, object>();
        public string MainGameId { get; set; } = string.Empty;
        public string DailyGameId { get; set; } = string.Empty;

        public Dictionary<string, object> ToPlayerDocument()
        {
            var progress = new Dictionary<string, object>
            {
                { "current_level", CurrentLevel },
                { "is_first_session", IsFirstSession },
                { "tutorial_done", TutorialDone },
                { "current_strategy", CurrentStrategy },
                { "consecutive_clean_wins", ConsecutiveCleanWins },
                { "last_level_clean_win", LastLevelCleanWin },
                { "consecutive_fails", ConsecutiveFails },
                { "consecutive_retry_levels", ConsecutiveRetryLevels },
                { "retry_tracking_strategy", RetryTrackingStrategy },
                { "daily_index", DailyIndex },
                { "daily_completed_date", DailyCompletedDate },
                { "max_daily_date", MaxDailyDate },
                { "daily_elapsed_sec", DailyElapsedSeconds },
                { "daily_beat_percent", DailyBeatPercent },
                { "daily_best_beat_percent", DailyBestBeatPercent },
                { "daily_started_date", DailyStartedDate },
                { "daily_first_easy_date", DailyFirstEasyDate },
                { "recent_win_counts_by_day", RecentWinCountsByDay },
                { "session_count", SessionCount },
                { "today_session_count", TodaySessionCount },
                { "last_day_session_count", LastDaySessionCount },
                { "active_days", ActiveDays },
                { "today_played_count", TodayPlayedCount },
                { "today_active_sec", TodayActiveSeconds },
                { "total_active_sec", TotalActiveSeconds },
                { "grt_level_d90_reported", ToObjects(GrtLevelD90Reported) },
                { "grt_reported_events", ToObjects(GrtReportedEvents) },
                { "first_open_time_ms", FirstOpenTimeMs },
                { "today_date", TodayDate },
                { "bank_progress", BankProgress },
                { "main_bank_progress", MainBankProgress },
                { "lkmod_progress", LkModifiedProgress },
                { "tool_locate", ToolLocate },
                { "tool_hint", ToolHint },
                { "tool_undo", ToolUndo },
                { "last_splash_date", LastSplashDate },
                { "has_used_tool", HasUsedTool },
                { "prop_highlight_shown", PropHighlightShown },
                { "push_ask_count", PushAskCount },
                { "push_guide_last_date", PushGuideLastDate },
                { "push_guide_shown_count", PushGuideShownCount },
                { "push_guide_popup_count", PushGuidePopupCount },
                { "has_shown_att_guide", HasShownAttGuide },
                { "apply_locale", AppliedLocale },
                { "music_on", MusicOn },
                { "music_user_modified", MusicUserModified },
                { "sound_on", SoundOn },
                { "vibration_on", VibrationOn },
                { "people_on", PeopleOn },
                { "pattern_mode_on", PatternModeOn },
                { "pattern_entry_dot_dismissed", PatternEntryDotDismissed },
                { "pattern_switch_dot_dismissed", PatternSwitchDotDismissed },
                { "has_used_revive_free", HasUsedReviveFree },
                { "interstitial_unlocked", InterstitialUnlocked },
                { "banner_unlocked", BannerUnlocked },
                { "last_win_beat_percent", LastWinBeatPercent },
                { "retry_puzzle_level", RetryPuzzleLevel },
                { "retry_puzzle_params", RetryPuzzleParameters },
                { "pre_cat_fail_lv", PreCatFailLevel },
                { "pre_cat_fail_count", PreCatFailCount },
                { "pre_cat_revived_this_level", PreCatRevivedThisLevel },
                { "pre_cat_pending_hard", PreCatPendingHard },
                { "pre_cat_pending_struggle", PreCatPendingStruggle },
                { "pre_cat_pending_demote", PreCatPendingDemote },
                { "pre_cat_lock_lv", PreCatLockLevel },
                { "pre_cat_lock_pre_type", PreCatLockType },
                {
                    "pre_cat_lock_pos",
                    new Dictionary<string, object>
                    {
                        { "x", PreCatLockPosition.x },
                        { "y", PreCatLockPosition.y }
                    }
                },
                { "recent_puzzles", RecentPuzzles },
                { "in_flight_awards", InFlightAwards },
                { "pending_rewards", PendingRewards },
                { "reward_history_ts", RewardHistoryTimestamps },
                { "restored_today_count", RestoredTodayCount },
                { "endgame_snapshot", new Dictionary<string, object>() },
                { "saved_game_auto_mark", SavedGameAutoMark },
                { "saved_ab_groups", SavedAbGroups },

                // The source keeps these legacy player-save keys empty because
                // live values are stored in the separate endgame file.
                { "main_game_total_stats", new Dictionary<string, object>() },
                { "daily_game_total_stats", new Dictionary<string, object>() },
                { "main_game_round_stats", new Dictionary<string, object>() },
                { "daily_game_round_stats", new Dictionary<string, object>() },
                { "main_game_id", string.Empty },
                { "daily_game_id", string.Empty }
            };

            return new Dictionary<string, object> { { "progress", progress } };
        }

        public Dictionary<string, object> ToEndgameDocument()
        {
            return new Dictionary<string, object>
            {
                {
                    "snapshot",
                    new Dictionary<string, object> { { "data", EndgameSnapshot } }
                },
                {
                    "stats",
                    new Dictionary<string, object>
                    {
                        { "main_total", MainGameTotalStats },
                        { "daily_total", DailyGameTotalStats },
                        { "main_round", MainGameRoundStats },
                        { "daily_round", DailyGameRoundStats },
                        { "main_id", MainGameId },
                        { "daily_id", DailyGameId }
                    }
                }
            };
        }

        public bool IsEndgameStoreEmpty()
        {
            return EndgameSnapshot.Count == 0 &&
                   MainGameTotalStats.Count == 0 &&
                   DailyGameTotalStats.Count == 0 &&
                   MainGameRoundStats.Count == 0 &&
                   DailyGameRoundStats.Count == 0 &&
                   string.IsNullOrEmpty(MainGameId) &&
                   string.IsNullOrEmpty(DailyGameId);
        }

        public static GameStateData FromDocuments(
            Dictionary<string, object> playerDocument,
            Dictionary<string, object> endgameDocument)
        {
            var data = new GameStateData();
            Dictionary<string, object> progress = Section(playerDocument, "progress");
            if (progress != null)
            {
                data.CurrentLevel = Int(progress, "current_level", 1);
                data.IsFirstSession = Bool(progress, "is_first_session", true);
                data.TutorialDone = Bool(progress, "tutorial_done", false);
                data.CurrentStrategy = Int(progress, "current_strategy", 1);
                data.ConsecutiveCleanWins = Int(progress, "consecutive_clean_wins", 0);
                data.LastLevelCleanWin = Bool(progress, "last_level_clean_win", false);
                data.ConsecutiveFails = Int(progress, "consecutive_fails", 0);
                data.ConsecutiveRetryLevels = Int(progress, "consecutive_retry_levels", 0);
                data.RetryTrackingStrategy = Int(progress, "retry_tracking_strategy", 0);
                data.DailyIndex = Int(progress, "daily_index", 0);
                data.DailyCompletedDate = String(
                    progress,
                    "daily_completed_date",
                    string.Empty);
                data.MaxDailyDate = String(
                    progress,
                    "max_daily_date",
                    string.Empty);
                data.DailyElapsedSeconds = Int(progress, "daily_elapsed_sec", 0);
                data.DailyBeatPercent = Float(progress, "daily_beat_percent", 0f);
                data.DailyBestBeatPercent = Float(
                    progress,
                    "daily_best_beat_percent",
                    0f);
                data.DailyStartedDate = String(
                    progress,
                    "daily_started_date",
                    string.Empty);
                data.DailyFirstEasyDate = String(progress, "daily_first_easy_date", string.Empty);
                data.RecentWinCountsByDay = Dictionary(progress, "recent_win_counts_by_day");
                data.SessionCount = Int(progress, "session_count", 0);
                data.TodaySessionCount = Int(progress, "today_session_count", 0);
                data.LastDaySessionCount = Int(progress, "last_day_session_count", 0);
                data.ActiveDays = Int(progress, "active_days", 0);
                data.TodayPlayedCount = Int(progress, "today_played_count", 0);
                data.TodayActiveSeconds = Int(progress, "today_active_sec", 0);
                data.TotalActiveSeconds = Int(progress, "total_active_sec", 0);
                data.GrtLevelD90Reported =
                    IntList(progress, "grt_level_d90_reported");
                data.GrtReportedEvents =
                    StringList(progress, "grt_reported_events");
                data.FirstOpenTimeMs = Long(
                    progress,
                    "first_open_time_ms",
                    0L);
                data.TodayDate = String(progress, "today_date", string.Empty);
                data.BankProgress = Dictionary(progress, "bank_progress");
                data.MainBankProgress = Dictionary(progress, "main_bank_progress");
                data.LkModifiedProgress = Dictionary(progress, "lkmod_progress");
                data.ToolLocate = Int(progress, "tool_locate", 5);
                data.ToolHint = Int(progress, "tool_hint", 5);
                data.ToolUndo = Int(progress, "tool_undo", 3);
                data.LastSplashDate = String(
                    progress,
                    "last_splash_date",
                    string.Empty);
                data.HasUsedTool = Bool(progress, "has_used_tool", false);
                data.PropHighlightShown = Bool(progress, "prop_highlight_shown", false);
                data.PushAskCount = Int(progress, "push_ask_count", 0);
                data.PushGuideLastDate = String(
                    progress,
                    "push_guide_last_date",
                    string.Empty);
                data.PushGuideShownCount = Int(
                    progress,
                    "push_guide_shown_count",
                    0);
                data.PushGuidePopupCount = Int(
                    progress,
                    "push_guide_popup_count",
                    0);
                data.HasShownAttGuide = Bool(
                    progress,
                    "has_shown_att_guide",
                    false);
                data.AppliedLocale = String(progress, "apply_locale", string.Empty);
                data.MusicOn = Bool(progress, "music_on", true);
                data.MusicUserModified = Bool(progress, "music_user_modified", false);
                data.SoundOn = Bool(progress, "sound_on", true);
                data.VibrationOn = Bool(progress, "vibration_on", true);
                data.PeopleOn = Bool(progress, "people_on", true);
                data.PatternModeOn = Bool(progress, "pattern_mode_on", false);
                data.PatternEntryDotDismissed = Bool(
                    progress,
                    "pattern_entry_dot_dismissed",
                    false);
                data.PatternSwitchDotDismissed = Bool(
                    progress,
                    "pattern_switch_dot_dismissed",
                    false);
                data.HasUsedReviveFree = Bool(
                    progress,
                    "has_used_revive_free",
                    false);
                data.InterstitialUnlocked = Bool(
                    progress,
                    "interstitial_unlocked",
                    false);
                data.BannerUnlocked = Bool(
                    progress,
                    "banner_unlocked",
                    false);
                data.LastWinBeatPercent = Float(
                    progress,
                    "last_win_beat_percent",
                    -1f);
                data.RetryPuzzleLevel = Int(progress, "retry_puzzle_level", 0);
                data.RetryPuzzleParameters = Dictionary(progress, "retry_puzzle_params");
                data.PreCatFailLevel = Int(progress, "pre_cat_fail_lv", 0);
                data.PreCatFailCount = Int(progress, "pre_cat_fail_count", 0);
                data.PreCatRevivedThisLevel = Bool(
                    progress,
                    "pre_cat_revived_this_level",
                    false);
                data.PreCatPendingHard = Bool(progress, "pre_cat_pending_hard", false);
                data.PreCatPendingStruggle = Bool(
                    progress,
                    "pre_cat_pending_struggle",
                    false);
                data.PreCatPendingDemote = Bool(progress, "pre_cat_pending_demote", false);
                data.PreCatLockLevel = Int(progress, "pre_cat_lock_lv", 0);
                data.PreCatLockType = String(progress, "pre_cat_lock_pre_type", "0");
                data.PreCatLockPosition = Position(progress, "pre_cat_lock_pos");
                data.RecentPuzzles = List(progress, "recent_puzzles");
                data.InFlightAwards = List(progress, "in_flight_awards");
                data.PendingRewards = List(progress, "pending_rewards");
                data.RewardHistoryTimestamps =
                    List(progress, "reward_history_ts");
                data.RestoredTodayCount = Int(
                    progress,
                    "restored_today_count",
                    0);
                data.SavedGameAutoMark = Int(progress, "saved_game_auto_mark", -1);
                data.SavedAbGroups = Dictionary(progress, "saved_ab_groups");
            }

            Dictionary<string, object> snapshot = Section(endgameDocument, "snapshot");
            Dictionary<string, object> stats = Section(endgameDocument, "stats");
            if (snapshot != null)
            {
                data.EndgameSnapshot = Dictionary(snapshot, "data");
            }
            if (stats != null)
            {
                data.MainGameTotalStats = Dictionary(stats, "main_total");
                data.DailyGameTotalStats = Dictionary(stats, "daily_total");
                data.MainGameRoundStats = Dictionary(stats, "main_round");
                data.DailyGameRoundStats = Dictionary(stats, "daily_round");
                data.MainGameId = String(stats, "main_id", string.Empty);
                data.DailyGameId = String(stats, "daily_id", string.Empty);
            }

            return data;
        }

        private static Dictionary<string, object> Section(
            Dictionary<string, object> document,
            string name)
        {
            if (document != null &&
                document.TryGetValue(name, out object value) &&
                value is Dictionary<string, object> section)
            {
                return section;
            }
            return null;
        }

        private static int Int(Dictionary<string, object> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null) return fallback;
            try { return Convert.ToInt32(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static long Long(
            Dictionary<string, object> values,
            string key,
            long fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null)
                return fallback;
            try { return Convert.ToInt64(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static bool Bool(Dictionary<string, object> values, string key, bool fallback)
        {
            return values.TryGetValue(key, out object value) && value is bool result
                ? result
                : fallback;
        }

        private static float Float(
            Dictionary<string, object> values,
            string key,
            float fallback)
        {
            if (!values.TryGetValue(key, out object value) || value == null)
                return fallback;
            try { return Convert.ToSingle(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static string String(
            Dictionary<string, object> values,
            string key,
            string fallback)
        {
            return values.TryGetValue(key, out object value) && value is string result
                ? result
                : fallback;
        }

        private static Dictionary<string, object> Dictionary(
            Dictionary<string, object> values,
            string key)
        {
            return values.TryGetValue(key, out object value) &&
                   value is Dictionary<string, object> result
                ? result
                : new Dictionary<string, object>();
        }

        private static List<object> List(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object value) && value is List<object> result
                ? result
                : new List<object>();
        }

        private static List<int> IntList(
            Dictionary<string, object> values,
            string key)
        {
            var result = new List<int>();
            List<object> source = List(values, key);
            for (int index = 0; index < source.Count; index++)
            {
                try { result.Add(Convert.ToInt32(source[index])); }
                catch (Exception) { }
            }
            return result;
        }

        private static List<string> StringList(
            Dictionary<string, object> values,
            string key)
        {
            var result = new List<string>();
            List<object> source = List(values, key);
            for (int index = 0; index < source.Count; index++)
                if (source[index] is string value &&
                    !string.IsNullOrEmpty(value))
                    result.Add(value);
            return result;
        }

        private static List<object> ToObjects<T>(IReadOnlyList<T> values)
        {
            var result = new List<object>();
            if (values == null) return result;
            for (int index = 0; index < values.Count; index++)
                result.Add(values[index]);
            return result;
        }

        private static Vector2Int Position(Dictionary<string, object> values, string key)
        {
            Dictionary<string, object> position = Dictionary(values, key);
            return position.Count == 0
                ? new Vector2Int(-1, -1)
                : new Vector2Int(Int(position, "x", -1), Int(position, "y", -1));
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/GameStateService.cs
``csharp
using System;
using System.Collections.Generic;
using System.Globalization;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Online;

namespace Meowdoku.Core
{
    public interface IVibrationStateSink
    {
        void SetEnabled(bool enabled);
    }

    public interface ICurrentDateProvider
    {
        string CurrentDate { get; }
    }

    public sealed class SystemCurrentDateProvider : ICurrentDateProvider
    {
        public static readonly SystemCurrentDateProvider Instance = new SystemCurrentDateProvider();
        private SystemCurrentDateProvider() { }
        public string CurrentDate => DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Runtime mutation slice ported from the bank-progress API in game_state.gd.
    /// </summary>
    public sealed class GameStateService :
        IDataSyncSavable,
        IDataSyncMergeBasis
    {
        private const int RecentPuzzlesLimit = 100;
        private const long RewardHistoryRetainSeconds = 7 * 24 * 3600;
        private const long RestoreNormalLookbackSeconds = 3 * 24 * 3600;
        private const int RestoreMinimumNormalRewards = 3;
        private const int RestoreDailyMaximum = 3;
        private readonly IGameStatePlayerStore _store;
        private readonly IGameStateEndgameStore _endgameStore;
        private readonly IVibrationStateSink _vibrationSink;
        private readonly string _applicationVersion;
        private readonly ICurrentDateProvider _dateProvider;
        private readonly DdaRankConfig _ddaRankConfig;
        private bool _dailyFirstEasyAvailable;
        private bool _dailyFirstEasyEvaluated;
        private bool _isCurrentLevelDailyFirstEasy;
        private bool _currentLevelDirty;
        private bool _currentLevelRetried;
        private bool _ddaToolOrReviveUsed;
        private bool _ddaReviveUsed;
        private bool _demotedThisLevel;
        private bool _ddaPendingDemote;
        private int _sessionPlayedCount;
        private int _sessionConsecutiveWins;
        private bool _hasWonSinceColdStart;
        private int _sessionRewardViewCount;
        private bool _firstSessionRuntime;
        private readonly Dictionary<int, float> _failTextRevivePercent = new();

        public GameStateService(
            GameStateData data,
            IGameStatePlayerStore store = null,
            IVibrationStateSink vibrationSink = null,
            IGameStateEndgameStore endgameStore = null,
            string applicationVersion = "",
            ICurrentDateProvider dateProvider = null,
            DdaRankConfig ddaRankConfig = null)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            _store = store;
            _endgameStore = endgameStore ?? store as IGameStateEndgameStore;
            _vibrationSink = vibrationSink;
            _applicationVersion = applicationVersion ?? string.Empty;
            _dateProvider = dateProvider ?? SystemCurrentDateProvider.Instance;
            _ddaRankConfig = ddaRankConfig ?? new DdaRankConfig();
            _firstSessionRuntime = Data.IsFirstSession;
            _vibrationSink?.SetEnabled(Data.VibrationOn);
        }

        public GameStateData Data { get; }
        public event Action<string, int> ToolCountChanged;

        public int CurrentLevel => Data.CurrentLevel;
        public bool TutorialDone => Data.TutorialDone;
        public bool IsFirstSession => _firstSessionRuntime;
        public int CurrentStrategy => Data.CurrentStrategy;
        public string CurrentDate => _dateProvider.CurrentDate;
        public string LastSplashDate => Data.LastSplashDate;
        public string AppliedLocale => Data.AppliedLocale;
        public bool MusicOn => Data.MusicOn;
        public bool SoundOn => Data.SoundOn;
        public bool VibrationOn => Data.VibrationOn;
        public bool PeopleOn => Data.PeopleOn;
        public bool PatternModeOn => Data.PatternModeOn;
        public bool PatternEntryDotDismissed => Data.PatternEntryDotDismissed;
        public bool PatternSwitchDotDismissed => Data.PatternSwitchDotDismissed;
        public bool HasUsedReviveFree => Data.HasUsedReviveFree;
        public float LastWinBeatPercent => Data.LastWinBeatPercent;
        public int DailyIndex => Data.DailyIndex;
        public string DailyCompletedDate => Data.DailyCompletedDate;
        public string MaxDailyDate => Data.MaxDailyDate;
        public int DailyElapsedSeconds => Data.DailyElapsedSeconds;
        public float DailyBeatPercent => Data.DailyBeatPercent;
        public float DailyBestBeatPercent => Data.DailyBestBeatPercent;
        public string DailyStartedDate => Data.DailyStartedDate;
        public DailyEntryState CurrentDailyEntryState =>
            DailyEntryStateContract.Compute(
                Data.CurrentLevel,
                _dateProvider.CurrentDate,
                Data.DailyCompletedDate,
                Data.MaxDailyDate);
        public bool HasUsedTool => Data.HasUsedTool;
        public bool HasPropHighlightShown => Data.PropHighlightShown;
        public int PushAskCount => Data.PushAskCount;
        public string PushGuideLastDate => Data.PushGuideLastDate;
        public int PushGuideShownCount => Data.PushGuideShownCount;
        public int PushGuidePopupCount => Data.PushGuidePopupCount;
        public bool HasShownAttGuide => Data.HasShownAttGuide;
        public bool IsCurrentLevelDirty => _currentLevelDirty;
        public bool IsCurrentLevelRetried => _currentLevelRetried;
        public bool WasDdaToolOrReviveUsed => _ddaToolOrReviveUsed;
        public bool WasDdaReviveUsed => _ddaReviveUsed;
        public int SessionPlayedCount => _sessionPlayedCount;
        public int SessionConsecutiveWins => _sessionConsecutiveWins;
        public bool HasWonSinceColdStart => _hasWonSinceColdStart;
        public bool InterstitialUnlocked => Data.InterstitialUnlocked;
        public bool BannerUnlocked => Data.BannerUnlocked;
        public int SessionRewardViewCount => _sessionRewardViewCount;
        public bool IsDailyFirstEasyAvailable => _dailyFirstEasyAvailable;
        public bool IsCurrentLevelDailyFirstEasy => _isCurrentLevelDailyFirstEasy;
        public event Action<bool> LevelSettled;

        public void EnsureFirstOpenTime(
            long sdkValueMilliseconds,
            long fallbackNowMilliseconds = 0)
        {
            if (Data.FirstOpenTimeMs > 0) return;
            Data.FirstOpenTimeMs = sdkValueMilliseconds > 0
                ? sdkValueMilliseconds
                : fallbackNowMilliseconds > 0
                    ? fallbackNowMilliseconds
                    : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            SavePlayer();
        }

        public void EvaluateDailyFirstEasy()
        {
            if (_dailyFirstEasyEvaluated) return;
            _dailyFirstEasyEvaluated = true;
            string today = _dateProvider.CurrentDate;
            if (string.CompareOrdinal(Data.DailyFirstEasyDate, today) >= 0)
            {
                _dailyFirstEasyAvailable = false;
                return;
            }

            Dictionary<string, object> snapshot = Data.EndgameSnapshot;
            if (snapshot.Count > 0 &&
                ReadObjectInt(snapshot, "level", 0) == Data.CurrentLevel &&
                ReadObjectInt(snapshot, "lives", 0) > 0)
            {
                if (HasValidPrefill(snapshot))
                {
                    Data.DailyFirstEasyDate = today;
                    _dailyFirstEasyAvailable = false;
                    SavePlayer();
                    return;
                }
                int prefill = CollectionCount(snapshot, "prefill_positions");
                int userCats = CollectionCount(snapshot, "placed_cats") - prefill;
                int marks = CollectionCount(snapshot, "marks");
                int errors = CollectionCount(snapshot, "errors");
                if (userCats > 0 || marks > 0 || errors > 0)
                {
                    Data.DailyFirstEasyDate = today;
                    _dailyFirstEasyAvailable = false;
                    SavePlayer();
                    return;
                }
            }
            _dailyFirstEasyAvailable = true;
        }

        public void ConsumeDailyFirstEasy(bool markCurrentLevel = false)
        {
            Data.DailyFirstEasyDate = _dateProvider.CurrentDate;
            _dailyFirstEasyAvailable = false;
            if (markCurrentLevel) _isCurrentLevelDailyFirstEasy = true;
            SavePlayer();
        }

        public void AdvanceDailyFirstEasyDate()
        {
            string today = _dateProvider.CurrentDate;
            if (string.CompareOrdinal(Data.DailyFirstEasyDate, today) >= 0) return;
            Data.DailyFirstEasyDate = today;
            _dailyFirstEasyAvailable = false;
            SavePlayer();
        }

        public void ResetCurrentLevelDailyFirstEasy()
        {
            _isCurrentLevelDailyFirstEasy = false;
        }

        public void SetDailyIndex(int value)
        {
            Data.DailyIndex = value;
            SavePlayer();
        }

        public void SetDailyStartedDate(string date)
        {
            Data.DailyStartedDate = date ?? string.Empty;
            SavePlayer();
        }

        public void AdvanceMaxDailyDate(string date = null)
        {
            string target = date ?? _dateProvider.CurrentDate;
            if (string.CompareOrdinal(target, Data.MaxDailyDate) <= 0) return;
            Data.MaxDailyDate = target;
            SavePlayer();
        }

        public void MarkDailyCompleted(
            string date,
            int elapsedSeconds,
            float beatPercent)
        {
            Data.DailyCompletedDate = date ?? string.Empty;
            Data.DailyElapsedSeconds = elapsedSeconds;
            Data.DailyBeatPercent = beatPercent;
            if (beatPercent > Data.DailyBestBeatPercent)
                Data.DailyBestBeatPercent = beatPercent;
            _hasWonSinceColdStart = true;
            SavePlayer();
        }

        public void ClearDailyCompletion()
        {
            Data.DailyCompletedDate = string.Empty;
            Data.DailyElapsedSeconds = 0;
            Data.DailyBeatPercent = 0f;
            Data.DailyBestBeatPercent = 0f;
            SavePlayer();
        }

        public void SetCurrentLevel(int value)
        {
            Data.CurrentLevel = value;
            SavePlayer();
        }

        public void SetTutorialDone(bool value)
        {
            Data.TutorialDone = value;
            SavePlayer();
        }

        public void ConsumeFirstSessionPersist()
        {
            if (!Data.IsFirstSession) return;
            Data.IsFirstSession = false;
            SavePlayer();
        }

        public void MarkFirstSessionDone()
        {
            _firstSessionRuntime = false;
        }

        public void SetCurrentStrategy(int value)
        {
            Data.CurrentStrategy = value;
            SavePlayer();
        }

        public bool MarkSplashShownToday()
        {
            string today = _dateProvider.CurrentDate;
            bool firstToday = !string.Equals(
                Data.LastSplashDate,
                today,
                StringComparison.Ordinal);
            if (!firstToday) return false;
            Data.LastSplashDate = today;
            SavePlayer();
            return true;
        }

        public void SetAppliedLocale(string value)
        {
            Data.AppliedLocale = value ?? string.Empty;
            SavePlayer();
        }

        public void SetMusicOn(bool value)
        {
            Data.MusicOn = value;
            Data.MusicUserModified = true;
            SavePlayer();
        }

        public void InitMusicDefault(bool defaultOn)
        {
            if (Data.MusicUserModified || Data.MusicOn == defaultOn) return;
            Data.MusicOn = defaultOn;
            SavePlayer();
        }

        public void SetSoundOn(bool value)
        {
            Data.SoundOn = value;
            SavePlayer();
        }

        public void SetVibrationOn(bool value)
        {
            Data.VibrationOn = value;
            _vibrationSink?.SetEnabled(value);
            SavePlayer();
        }

        public void SetPeopleOn(bool value)
        {
            Data.PeopleOn = value;
            SavePlayer();
        }

        public void SetPatternModeOn(bool value)
        {
            Data.PatternModeOn = value;
            SavePlayer();
        }

        public void MarkPatternEntryDotDismissed()
        {
            if (Data.PatternEntryDotDismissed) return;
            Data.PatternEntryDotDismissed = true;
            SavePlayer();
        }

        public void MarkPatternSwitchDotDismissed()
        {
            if (Data.PatternSwitchDotDismissed) return;
            Data.PatternSwitchDotDismissed = true;
            SavePlayer();
        }

        public void MarkReviveFreeUsed()
        {
            if (Data.HasUsedReviveFree) return;
            Data.HasUsedReviveFree = true;
            SavePlayer();
        }

        public void SetLastWinBeatPercent(float value)
        {
            if (Math.Abs(Data.LastWinBeatPercent - value) < 0.0001f) return;
            Data.LastWinBeatPercent = value;
            SavePlayer();
        }

        public float GetFailTextRevivePercent(int level)
        {
            return _failTextRevivePercent.TryGetValue(level, out float value)
                ? value
                : -1f;
        }

        public void SetFailTextRevivePercent(int level, float value)
        {
            _failTextRevivePercent[level] = value;
        }

        public int GetToolCount(string kind)
        {
            switch (kind)
            {
                case "locate": return Data.ToolLocate;
                case "hint": return Data.ToolHint;
                default: return 0;
            }
        }

        public void SetToolCount(string kind, int count)
        {
            int previous = GetToolCount(kind);
            switch (kind)
            {
                case "locate": Data.ToolLocate = count; break;
                case "hint": Data.ToolHint = count; break;
                default: return;
            }

            if (count < previous && !Data.HasUsedTool)
                Data.HasUsedTool = true;
            SavePlayer();
            ToolCountChanged?.Invoke(kind, count);
        }

        public List<object> GetInFlightAwards()
        {
            return new List<object>(Data.InFlightAwards);
        }

        public void AddInFlightAward(Dictionary<string, object> entry)
        {
            if (entry == null) return;
            Data.InFlightAwards.Add(entry);
            SavePlayer();
        }

        public bool RemoveInFlightAward(int uid)
        {
            for (int index = Data.InFlightAwards.Count - 1;
                 index >= 0;
                 index--)
            {
                if (Data.InFlightAwards[index] is not
                        Dictionary<string, object> entry ||
                    ReadObjectInt(entry, "uid", -1) != uid)
                    continue;
                Data.InFlightAwards.RemoveAt(index);
                SavePlayer();
                return true;
            }
            return false;
        }

        public Dictionary<string, object> FindInFlightAward(int uid)
        {
            foreach (object value in Data.InFlightAwards)
            {
                if (value is Dictionary<string, object> entry &&
                    ReadObjectInt(entry, "uid", -1) == uid)
                    return entry;
            }
            return null;
        }

        public void MarkPropHighlightShown()
        {
            if (Data.PropHighlightShown) return;
            Data.PropHighlightShown = true;
            SavePlayer();
        }

        public void IncrementPushAskCount()
        {
            Data.PushAskCount++;
            SavePlayer();
        }

        public void MarkPushGuideTriggered()
        {
            Data.PushGuideLastDate = _dateProvider.CurrentDate;
            Data.PushGuideShownCount++;
            SavePlayer();
        }

        public void MarkPushGuidePopupShown()
        {
            Data.PushGuidePopupCount++;
            SavePlayer();
        }

        public bool IsPushGuideCooldownElapsed()
        {
            if (string.IsNullOrEmpty(Data.PushGuideLastDate)) return true;
            if (!DateTime.TryParseExact(
                    Data.PushGuideLastDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime lastDate))
                return true;
            if (!DateTime.TryParseExact(
                    _dateProvider.CurrentDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime today))
                return true;
            return (today - lastDate).TotalDays >= 5d;
        }

        public int GetRecentThreeDayWinCount()
        {
            RollDayIfNeeded();
            int total = 0;
            foreach (object value in Data.RecentWinCountsByDay.Values)
            {
                try { total += Convert.ToInt32(value); }
                catch (Exception exception) when (
                    exception is FormatException ||
                    exception is InvalidCastException ||
                    exception is OverflowException) { }
            }
            return total;
        }

        public void MarkAttGuideShown()
        {
            if (Data.HasShownAttGuide) return;
            Data.HasShownAttGuide = true;
            SavePlayer();
        }

        public void MarkCurrentLevelDirty()
        {
            _currentLevelDirty = true;
        }

        public void ClearCurrentLevelDirty()
        {
            _currentLevelDirty = false;
        }

        public void MarkDdaToolOrReviveUsed()
        {
            _ddaToolOrReviveUsed = true;
        }

        public void MarkDdaReviveUsed()
        {
            _ddaReviveUsed = true;
        }

        public void ResetCurrentLevelRuntimeFlags()
        {
            _currentLevelDirty = false;
            _ddaToolOrReviveUsed = false;
            _ddaReviveUsed = false;
        }

        public void OnSessionStarted()
        {
            RollDayIfNeeded();
            Data.SessionCount++;
            Data.TodaySessionCount++;
            _sessionPlayedCount = 0;
            _sessionConsecutiveWins = 0;
            _sessionRewardViewCount = 0;
            SavePlayer();
        }

        public void IncrementSessionRewardViewCount()
        {
            _sessionRewardViewCount++;
        }

        public void ResetSessionRewardViewCount()
        {
            _sessionRewardViewCount = 0;
        }

        public void MarkInterstitialUnlocked()
        {
            if (Data.InterstitialUnlocked) return;
            Data.InterstitialUnlocked = true;
            SavePlayer();
        }

        public void MarkBannerUnlocked()
        {
            if (Data.BannerUnlocked) return;
            Data.BannerUnlocked = true;
            SavePlayer();
        }

        public bool HasPendingRewards() => Data.PendingRewards.Count > 0;

        public List<object> GetPendingRewards() =>
            new(Data.PendingRewards);

        public void AddPendingReward(Dictionary<string, object> reward)
        {
            if (reward == null) return;
            Data.PendingRewards.Add(reward);
            SavePlayer();
        }

        public List<object> PopAllPendingRewards()
        {
            var result = new List<object>(Data.PendingRewards);
            if (result.Count == 0) return result;
            Data.PendingRewards.Clear();
            SavePlayer();
            return result;
        }

        public void RemovePendingRewards(IReadOnlyCollection<string> showIds)
        {
            if (showIds == null || showIds.Count == 0) return;
            bool changed = false;
            for (int index = Data.PendingRewards.Count - 1; index >= 0; index--)
            {
                if (Data.PendingRewards[index] is not
                        Dictionary<string, object> entry ||
                    !Contains(showIds, ReadString(entry, "show_id")))
                    continue;
                Data.PendingRewards.RemoveAt(index);
                changed = true;
            }
            if (changed) SavePlayer();
        }

        public void RemovePendingRewardEntries(
            IReadOnlyCollection<object> entries)
        {
            if (entries == null || entries.Count == 0) return;
            bool changed = false;
            foreach (object entry in entries)
                changed |= Data.PendingRewards.Remove(entry);
            if (changed) SavePlayer();
        }

        public void RecordNormalReward(long unixTimestamp)
        {
            Data.RewardHistoryTimestamps.Add(unixTimestamp);
            long cutoff = unixTimestamp - RewardHistoryRetainSeconds;
            for (int index = Data.RewardHistoryTimestamps.Count - 1;
                 index >= 0;
                 index--)
            {
                if (ReadLong(Data.RewardHistoryTimestamps[index]) < cutoff)
                    Data.RewardHistoryTimestamps.RemoveAt(index);
            }
            SavePlayer();
        }

        public int GetRestoreRemainingToday(long unixTimestamp)
        {
            RollDayIfNeeded();
            long cutoff = unixTimestamp - RestoreNormalLookbackSeconds;
            int recent = 0;
            for (int index = 0;
                 index < Data.RewardHistoryTimestamps.Count;
                 index++)
            {
                if (ReadLong(Data.RewardHistoryTimestamps[index]) >= cutoff)
                    recent++;
            }
            if (recent < RestoreMinimumNormalRewards) return 0;
            return Math.Max(
                0,
                RestoreDailyMaximum - Data.RestoredTodayCount);
        }

        public int RestoredTodayCount
        {
            get
            {
                RollDayIfNeeded();
                return Data.RestoredTodayCount;
            }
        }

        public void AddRestoredTodayCount(int count)
        {
            if (count <= 0) return;
            RollDayIfNeeded();
            Data.RestoredTodayCount += count;
            SavePlayer();
        }

        public void AddActiveSeconds(int seconds)
        {
            if (seconds <= 0) return;
            RollDayIfNeeded();
            Data.TodayActiveSeconds += seconds;
            Data.TotalActiveSeconds += seconds;
            SavePlayer();
        }

        public bool HasGrtLevelD90Reported(int level) =>
            Data.GrtLevelD90Reported.Contains(level);

        public void MarkGrtLevelD90Reported(int level)
        {
            if (level <= 0 || HasGrtLevelD90Reported(level)) return;
            Data.GrtLevelD90Reported.Add(level);
            SavePlayer();
        }

        public bool HasGrtEventReported(string eventName) =>
            !string.IsNullOrEmpty(eventName) &&
            Data.GrtReportedEvents.Contains(eventName);

        public void MarkGrtEventReported(string eventName)
        {
            if (string.IsNullOrEmpty(eventName) ||
                HasGrtEventReported(eventName))
                return;
            Data.GrtReportedEvents.Add(eventName);
            SavePlayer();
        }

        public void OnGameFinished()
        {
            RollDayIfNeeded();
            _sessionPlayedCount++;
            Data.TodayPlayedCount++;
            SavePlayer();
        }

        public void OnLevelWon(int levelNumber)
        {
            int nextLevel = levelNumber + 1;
            if (nextLevel > Data.CurrentLevel) Data.CurrentLevel = nextLevel;

            int strategyBefore = Data.CurrentStrategy;
            Data.PreCatPendingStruggle =
                (Data.PreCatFailLevel == levelNumber && Data.PreCatFailCount >= 2) ||
                Data.PreCatRevivedThisLevel;
            Data.PreCatFailCount = 0;
            Data.PreCatFailLevel = 0;
            Data.PreCatRevivedThisLevel = false;
            Data.PreCatLockLevel = 0;
            Data.PreCatLockType = "0";
            Data.PreCatLockPosition = new UnityEngine.Vector2Int(-1, -1);
            Data.PreCatPendingHard = LevelData.IsHardLevel(levelNumber);

            if (levelNumber >= 6)
            {
                int maxStrategy;
                if (levelNumber >= 201) maxStrategy = 6;
                else if (levelNumber >= 101) maxStrategy = 5;
                else if (levelNumber >= 51) maxStrategy = 4;
                else if (levelNumber >= 21) maxStrategy = 3;
                else maxStrategy = 2;

                int winThreshold = levelNumber >= 51 ? 1 : 2;
                int minStrategy = levelNumber >= 101 ? 2 : 1;
                bool cleanWin = !_currentLevelDirty;
                if (cleanWin)
                {
                    Data.ConsecutiveCleanWins++;
                    if (Data.ConsecutiveCleanWins >= winThreshold &&
                        Data.CurrentStrategy < maxStrategy)
                    {
                        Data.CurrentStrategy++;
                        Data.ConsecutiveCleanWins = 0;
                    }
                }
                else
                {
                    Data.ConsecutiveCleanWins = 0;
                }

                int failThreshold = levelNumber >= 21 ? 2 : 1;
                if (Data.ConsecutiveFails >= failThreshold &&
                    Data.CurrentStrategy > minStrategy &&
                    !_demotedThisLevel)
                {
                    Data.CurrentStrategy--;
                    _demotedThisLevel = true;
                }
                Data.ConsecutiveFails = 0;

                if (levelNumber >= 21)
                {
                    if (_currentLevelRetried)
                    {
                        if (Data.CurrentStrategy == Data.RetryTrackingStrategy)
                        {
                            Data.ConsecutiveRetryLevels++;
                            int retryMinimum = levelNumber >= 101 ? 2 : 1;
                            if (Data.ConsecutiveRetryLevels >= 2 &&
                                Data.CurrentStrategy > retryMinimum &&
                                !_demotedThisLevel)
                            {
                                Data.CurrentStrategy--;
                                Data.ConsecutiveRetryLevels = 0;
                                Data.RetryTrackingStrategy = 0;
                            }
                        }
                        else
                        {
                            Data.ConsecutiveRetryLevels = 1;
                            Data.RetryTrackingStrategy = Data.CurrentStrategy;
                        }
                    }
                    else
                    {
                        Data.ConsecutiveRetryLevels = 0;
                        Data.RetryTrackingStrategy = 0;
                    }
                }

                ApplyDdaDemoteOnWon(levelNumber, minStrategy);
            }

            Data.LastLevelCleanWin = !_currentLevelDirty;
            _currentLevelRetried = false;
            _currentLevelDirty = false;
            _ddaToolOrReviveUsed = false;
            _ddaReviveUsed = false;
            _isCurrentLevelDailyFirstEasy = false;
            _demotedThisLevel = false;
            Data.RetryPuzzleLevel = 0;
            Data.RetryPuzzleParameters = new Dictionary<string, object>();
            _hasWonSinceColdStart = true;
            _sessionConsecutiveWins++;
            IncrementTodayWinCount();
            if (Data.CurrentStrategy < strategyBefore)
                Data.PreCatPendingDemote = true;

            SavePlayer();
            LevelSettled?.Invoke(true);
        }

        public void OnLevelFailed(int levelNumber)
        {
            _currentLevelRetried = true;
            _currentLevelDirty = true;
            Data.LastLevelCleanWin = false;
            _sessionConsecutiveWins = 0;

            if (levelNumber != Data.PreCatFailLevel)
            {
                Data.PreCatFailLevel = levelNumber;
                Data.PreCatFailCount = 0;
                Data.PreCatRevivedThisLevel = false;
            }
            Data.PreCatFailCount++;

            if (levelNumber >= 6)
            {
                Data.ConsecutiveCleanWins = 0;
                Data.ConsecutiveFails++;
            }
            if (_ddaRankConfig.IsAnyActionDemote())
                _ddaToolOrReviveUsed = true;

            SavePlayer();
            LevelSettled?.Invoke(false);
        }

        private void ApplyDdaDemoteOnWon(int levelNumber, int minimumStrategy)
        {
            if (!_ddaRankConfig.IsRetryOnceDemote() &&
                !_ddaRankConfig.IsToolReviveDemote() &&
                !_ddaRankConfig.IsAnyActionDemote())
                return;
            if (_isCurrentLevelDailyFirstEasy) return;

            bool triggered;
            if (_ddaRankConfig.IsRetryOnceDemote())
                triggered = _currentLevelRetried || _ddaReviveUsed;
            else
                triggered = _ddaToolOrReviveUsed;

            int nextLevel = levelNumber + 1;
            bool nextIsSkip = LevelData.IsHardLevel(nextLevel) ||
                              LevelData.IsSpecialLevel(nextLevel);

            if (_ddaPendingDemote && !_demotedThisLevel)
            {
                Data.CurrentStrategy = Math.Max(minimumStrategy, Data.CurrentStrategy - 1);
                _ddaPendingDemote = false;
                _demotedThisLevel = true;
            }
            if (!triggered || _demotedThisLevel) return;

            if (nextIsSkip)
                _ddaPendingDemote = true;
            else
            {
                Data.CurrentStrategy = Math.Max(minimumStrategy, Data.CurrentStrategy - 1);
                _demotedThisLevel = true;
            }
        }

        private void RollDayIfNeeded()
        {
            string today = _dateProvider.CurrentDate;
            if (Data.TodayDate == today) return;

            Data.LastDaySessionCount = Data.TodaySessionCount;
            Data.TodaySessionCount = 0;
            Data.TodayPlayedCount = 0;
            Data.TodayActiveSeconds = 0;
            Data.RestoredTodayCount = 0;
            Data.ActiveDays++;

            if (DateTime.TryParseExact(
                    today,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime currentDate))
            {
                DateTime cutoff = currentDate.AddDays(-2);
                var stale = new List<string>();
                foreach (string key in Data.RecentWinCountsByDay.Keys)
                {
                    if (DateTime.TryParseExact(
                            key,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime value) && value < cutoff)
                        stale.Add(key);
                }
                for (int index = 0; index < stale.Count; index++)
                    Data.RecentWinCountsByDay.Remove(stale[index]);
            }
            Data.TodayDate = today;
        }

        private void IncrementTodayWinCount()
        {
            string today = _dateProvider.CurrentDate;
            Data.RecentWinCountsByDay[today] =
                ReadInt(Data.RecentWinCountsByDay, today, 0) + 1;
        }

        public void SetRetryPuzzle(int level, Dictionary<string, object> parameters)
        {
            Data.RetryPuzzleLevel = level;
            Data.RetryPuzzleParameters = parameters ?? new Dictionary<string, object>();
            SavePlayer();
        }

        public Dictionary<string, object> GetRetryPuzzle(int level)
        {
            return Data.RetryPuzzleLevel == level && Data.RetryPuzzleParameters.Count > 0
                ? Data.RetryPuzzleParameters
                : new Dictionary<string, object>();
        }

        public int GetPreCatFailCount(int level)
        {
            return Data.PreCatFailLevel == level ? Data.PreCatFailCount : 0;
        }

        public void MarkPreCatRevived()
        {
            if (Data.PreCatRevivedThisLevel) return;
            Data.PreCatRevivedThisLevel = true;
            SavePlayer();
        }

        public Dictionary<string, object> ConsumePreCatPending()
        {
            var result = new Dictionary<string, object>
            {
                { "hard", Data.PreCatPendingHard },
                { "struggle", Data.PreCatPendingStruggle },
                { "demote", Data.PreCatPendingDemote }
            };

            if (!Data.PreCatPendingHard &&
                !Data.PreCatPendingStruggle &&
                !Data.PreCatPendingDemote)
                return result;

            Data.PreCatPendingHard = false;
            Data.PreCatPendingStruggle = false;
            Data.PreCatPendingDemote = false;
            SavePlayer();
            return result;
        }

        public Dictionary<string, object> GetPreCatLock(int level)
        {
            if (level > 0 && Data.PreCatLockLevel == level)
            {
                return new Dictionary<string, object>
                {
                    { "locked", true },
                    { "pre_type", Data.PreCatLockType },
                    { "position", Data.PreCatLockPosition }
                };
            }

            return new Dictionary<string, object>
            {
                { "locked", false },
                { "pre_type", "0" },
                { "position", new UnityEngine.Vector2Int(-1, -1) }
            };
        }

        public void SetPreCatLock(
            int level,
            string preType,
            UnityEngine.Vector2Int position)
        {
            Data.PreCatLockLevel = level;
            Data.PreCatLockType = preType ?? "0";
            Data.PreCatLockPosition = position;
            SavePlayer();
        }

        public Dictionary<string, object> RecordPuzzle(
            string puzzleId,
            int level,
            string version = "",
            string source = "")
        {
            Dictionary<string, object> previous = null;
            for (int index = Data.RecentPuzzles.Count - 1; index >= 0; index--)
            {
                if (!(Data.RecentPuzzles[index] is Dictionary<string, object> entry)) continue;
                if (ReadString(entry, "puzzle_id") == (puzzleId ?? string.Empty))
                {
                    previous = DeepClone(entry);
                    break;
                }
            }

            Data.RecentPuzzles.Add(new Dictionary<string, object>
            {
                { "puzzle_id", puzzleId ?? string.Empty },
                { "level", level },
                { "v", version ?? string.Empty },
                { "src", source ?? string.Empty },
                { "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
                { "bank_progress", DeepClone(Data.BankProgress) },
                { "main_bank_progress", DeepClone(Data.MainBankProgress) },
                { "lkmod_progress", DeepClone(Data.LkModifiedProgress) }
            });
            while (Data.RecentPuzzles.Count > RecentPuzzlesLimit) Data.RecentPuzzles.RemoveAt(0);
            SavePlayer();
            return previous ?? new Dictionary<string, object>();
        }

        public List<object> GetRecentPuzzles()
        {
            return (List<object>)DeepCloneValue(Data.RecentPuzzles);
        }

        public Dictionary<string, object> GetEndgameSnapshot()
        {
            return Data.EndgameSnapshot;
        }

        public bool SetEndgameSnapshot(Dictionary<string, object> snapshot)
        {
            snapshot = snapshot ?? new Dictionary<string, object>();
            if (snapshot.Count > 0) snapshot["app_version"] = _applicationVersion;
            Data.EndgameSnapshot = snapshot;
            return SaveEndgameNow();
        }

        public bool ClearEndgameSnapshot()
        {
            if (Data.EndgameSnapshot.Count == 0) return true;
            Data.EndgameSnapshot = new Dictionary<string, object>();
            return SaveEndgameNow();
        }

        public int GetGameTotalStat(string gameType, string key)
        {
            return ReadInt(TotalStats(gameType), key, 0);
        }

        public bool IncrementGameTotalStat(string gameType, string key, int delta = 1)
        {
            Dictionary<string, object> stats = TotalStats(gameType);
            stats[key] = ReadInt(stats, key, 0) + delta;
            return RequestEndgameSave();
        }

        public string GetPersistedGameId(string gameType)
        {
            return gameType == "daily" ? Data.DailyGameId : Data.MainGameId;
        }

        public bool SetPersistedGameId(string gameType, string value)
        {
            if (gameType == "daily") Data.DailyGameId = value ?? string.Empty;
            else Data.MainGameId = value ?? string.Empty;
            return SaveEndgameNow();
        }

        public bool ResetGameTotalStats(string gameType)
        {
            Dictionary<string, object> stats = TotalStats(gameType);
            if (stats.Count == 0) return true;
            stats.Clear();
            return SaveEndgameNow();
        }

        public Dictionary<string, object> GetGameRoundStats(string gameType)
        {
            return new Dictionary<string, object>(RoundStats(gameType));
        }

        public bool PersistGameRoundStats(
            string gameType,
            Dictionary<string, object> stats)
        {
            Dictionary<string, object> copy = stats == null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(stats);
            if (gameType == "daily") Data.DailyGameRoundStats = copy;
            else Data.MainGameRoundStats = copy;
            return RequestEndgameSave();
        }

        public bool ResetGameRoundStats(string gameType)
        {
            Dictionary<string, object> stats = RoundStats(gameType);
            if (stats.Count == 0) return true;
            stats.Clear();
            return SaveEndgameNow();
        }

        public int GetBankIndex(int size, int rank, string tier = "")
        {
            string key = ProgressKey(size, rank, tier);
            return ReadInt(Data.BankProgress, key, 0);
        }

        public void AdvanceBankIndex(
            int size,
            int rank,
            string tier = "",
            bool persist = true)
        {
            string key = ProgressKey(size, rank, tier);
            Data.BankProgress[key] = ReadInt(Data.BankProgress, key, 0) + 1;
            if (persist) SavePlayer();
        }

        public Dictionary<string, object> GetMainProgress(
            int size,
            int rank,
            string tier = "")
        {
            string key = ProgressKey(size, rank, tier);
            if (!Data.MainBankProgress.TryGetValue(key, out object raw) ||
                !(raw is Dictionary<string, object> progress))
            {
                // This legacy-shaped default is intentional. get_next_entry_main in
                // the source detects the absent "idx" and migrates bank_progress.
                progress = new Dictionary<string, object>
                {
                    { "lk_mod", 0 },
                    { "regular", 0 },
                    { "lkstyle", 0 },
                    { "transform", 0 }
                };
                Data.MainBankProgress[key] = progress;
            }
            return progress;
        }

        public void SetMainProgress(
            int size,
            int rank,
            string tier,
            Dictionary<string, object> progress,
            bool persist = true)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            Data.MainBankProgress[ProgressKey(size, rank, tier)] = progress;
            if (persist) SavePlayer();
        }

        public Dictionary<string, object> GetLkModifiedProgress(int size, int rank)
        {
            string key = LkModifiedProgressKey(size, rank);
            if (!Data.LkModifiedProgress.TryGetValue(key, out object raw) ||
                !(raw is Dictionary<string, object> progress))
            {
                progress = new Dictionary<string, object> { { "idx", 0 } };
                Data.LkModifiedProgress[key] = progress;
            }
            return progress;
        }

        public void SetLkModifiedProgress(
            int size,
            int rank,
            Dictionary<string, object> progress,
            bool persist = true)
        {
            if (progress == null) throw new ArgumentNullException(nameof(progress));
            Data.LkModifiedProgress[LkModifiedProgressKey(size, rank)] = progress;
            if (persist) SavePlayer();
        }

        public bool CommitBankProgress()
        {
            return SavePlayer();
        }

        public Dictionary<string, object> GetBankProgressSnapshot()
        {
            return DeepClone(Data.BankProgress);
        }

        public Dictionary<string, object> GetMainBankProgressSnapshot()
        {
            return DeepClone(Data.MainBankProgress);
        }

        public Dictionary<string, object> GetLkModifiedProgressSnapshot()
        {
            return DeepClone(Data.LkModifiedProgress);
        }

        public static string ProgressKey(int size, int rank, string tier = "")
        {
            return $"{size}_{rank}{(tier == "H" ? "_H" : string.Empty)}";
        }

        public static string LkModifiedProgressKey(int size, int rank)
        {
            return $"{size}_{rank}";
        }

        public string RemoteSaveId => "core";

        public bool IsRemoteAhead(
            IReadOnlyDictionary<string, object> remote)
        {
            return DataSyncValues.Int(remote, "current_level") >
                   Data.CurrentLevel;
        }

        public Dictionary<string, object> ExportRemote()
        {
            return new Dictionary<string, object>
            {
                ["current_level"] = Data.CurrentLevel,
                ["tool_locate"] = Data.ToolLocate,
                ["tool_hint"] = Data.ToolHint,
                ["current_strategy"] = Data.CurrentStrategy
            };
        }

        public bool MergeRemote(
            IReadOnlyDictionary<string, object> remote,
            DataSyncMergeContext context)
        {
            if (remote == null || remote.Count == 0 ||
                !context.RemoteAhead)
                return false;

            Data.CurrentLevel = DataSyncValues.Int(
                remote,
                "current_level",
                Data.CurrentLevel);
            Data.CurrentStrategy = DataSyncValues.Int(
                remote,
                "current_strategy",
                Data.CurrentStrategy);
            if (Data.CurrentLevel > 1 && !Data.TutorialDone)
                Data.TutorialDone = true;

            int locate = DataSyncValues.Int(
                remote,
                "tool_locate",
                Data.ToolLocate);
            int hint = DataSyncValues.Int(
                remote,
                "tool_hint",
                Data.ToolHint);
            bool locateChanged = locate != Data.ToolLocate;
            bool hintChanged = hint != Data.ToolHint;
            Data.ToolLocate = locate;
            Data.ToolHint = hint;
            SavePlayer();

            if (locateChanged)
                ToolCountChanged?.Invoke("locate", Data.ToolLocate);
            if (hintChanged)
                ToolCountChanged?.Invoke("hint", Data.ToolHint);
            return true;
        }

        private bool SavePlayer()
        {
            return _store == null || _store.SavePlayer(Data);
        }

        private bool SaveEndgameNow()
        {
            return _endgameStore == null || _endgameStore.SaveEndgame(Data);
        }

        private bool RequestEndgameSave()
        {
            return _endgameStore == null || _endgameStore.RequestSaveEndgame(Data);
        }

        private Dictionary<string, object> TotalStats(string gameType)
        {
            return gameType == "daily"
                ? Data.DailyGameTotalStats
                : Data.MainGameTotalStats;
        }

        private Dictionary<string, object> RoundStats(string gameType)
        {
            return gameType == "daily"
                ? Data.DailyGameRoundStats
                : Data.MainGameRoundStats;
        }

        private static int ReadInt(
            Dictionary<string, object> values,
            string key,
            int fallback)
        {
            if (!values.TryGetValue(key, out object raw) || raw == null) return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static string ReadString(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object raw) && raw != null ? raw.ToString() : string.Empty;
        }

        private static int ReadObjectInt(Dictionary<string, object> values, string key, int fallback)
        {
            if (!values.TryGetValue(key, out object raw) || raw == null) return fallback;
            try { return Convert.ToInt32(raw); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return fallback;
            }
        }

        private static long ReadLong(object value)
        {
            if (value == null) return 0;
            try { return Convert.ToInt64(value); }
            catch (Exception exception) when (
                exception is FormatException ||
                exception is InvalidCastException ||
                exception is OverflowException)
            {
                return 0;
            }
        }

        private static bool Contains(
            IReadOnlyCollection<string> values,
            string target)
        {
            foreach (string value in values)
                if (string.Equals(value, target, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private static int CollectionCount(Dictionary<string, object> values, string key)
        {
            return values.TryGetValue(key, out object raw) && raw is System.Collections.ICollection collection
                ? collection.Count
                : 0;
        }

        private static bool HasValidPrefill(Dictionary<string, object> snapshot)
        {
            if (!snapshot.TryGetValue("prefill_positions", out object rawPositions) ||
                !(rawPositions is System.Collections.IList positions) || positions.Count == 0 ||
                !snapshot.TryGetValue("solution", out object rawSolution) ||
                !(rawSolution is System.Collections.IList solution) || solution.Count == 0)
                return false;
            for (int i = 0; i < positions.Count; i++)
            {
                if (!(positions[i] is System.Collections.IList position) || position.Count < 2) return false;
                int row = Convert.ToInt32(position[0]);
                int column = Convert.ToInt32(position[1]);
                if (row < 0 || row >= solution.Count || Convert.ToInt32(solution[row]) != column) return false;
            }
            return true;
        }

        private static Dictionary<string, object> DeepClone(
            Dictionary<string, object> source)
        {
            var clone = new Dictionary<string, object>(source.Count);
            foreach (KeyValuePair<string, object> pair in source)
            {
                clone[pair.Key] = DeepCloneValue(pair.Value);
            }
            return clone;
        }

        private static object DeepCloneValue(object value)
        {
            if (value is Dictionary<string, object> dictionary) return DeepClone(dictionary);
            if (value is List<object> list)
            {
                var clone = new List<object>(list.Count);
                foreach (object item in list) clone.Add(DeepCloneValue(item));
                return clone;
            }
            return value;
        }
    }

    public static class GameStateRuntime
    {
        private static GameStateService _current;
        private static GameStateRepository _repository;
        private static bool _quittingHookRegistered;

        public static GameStateService Current
        {
            get
            {
                if (_current != null) return _current;
                GameStateRepository repository = GameStateRepository.CreateDefault();
                _repository = repository;
                _current = new GameStateService(
                    repository.Load(),
                    repository,
                    null,
                    repository,
                    UnityEngine.Application.version);
                RegisterQuittingHook();
                return _current;
            }
        }

        public static void Configure(GameStateService service)
        {
            FlushPendingWrites();
            _repository = null;
            _current = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static bool FlushPendingWrites()
        {
            return _repository == null || _repository.FlushEndgameWrites();
        }

#if UNITY_INCLUDE_TESTS
        /// <summary>
        /// Temporarily replaces the process-wide runtime state without
        /// flushing, replacing, or otherwise touching the repository that
        /// owns the player's real save. The exact previous references are
        /// restored when the returned scope is disposed.
        /// </summary>
        internal static IDisposable OverrideForTests(GameStateService service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            var scope = new TestOverrideScope(
                _current,
                _repository,
                service);
            _current = service;
            _repository = null;
            return scope;
        }

        private sealed class TestOverrideScope : IDisposable
        {
            private readonly GameStateService _previousCurrent;
            private readonly GameStateRepository _previousRepository;
            private readonly GameStateService _replacement;
            private bool _disposed;

            public TestOverrideScope(
                GameStateService previousCurrent,
                GameStateRepository previousRepository,
                GameStateService replacement)
            {
                _previousCurrent = previousCurrent;
                _previousRepository = previousRepository;
                _replacement = replacement;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                if (!ReferenceEquals(_current, _replacement))
                    throw new InvalidOperationException(
                        "GameStateRuntime test overrides must be disposed in order.");
                _current = _previousCurrent;
                _repository = _previousRepository;
            }
        }
#endif

        private static void RegisterQuittingHook()
        {
            if (_quittingHookRegistered) return;
            UnityEngine.Application.quitting += HandleApplicationQuitting;
            _quittingHookRegistered = true;
        }

        private static void HandleApplicationQuitting()
        {
            FlushPendingWrites();
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/UI/UIContracts.cs
``csharp
using System;

namespace Meowdoku.Core.UI
{
    // Values mirror scripts/module/ui/ui_name.gd. The enum is intentionally
    // stable because registry assets serialize it by integer.
    public enum UiName
    {
        Splash = 0,
        Home = 1,
        Game = 2,
        Bank = 3,
        Tutorial = 4,
        Setting = 5,
        Language = 6,
        HowToPlay = 7,
        HowToPlayPaged = 8,
        DailyGame = 9,
        Feedback = 10,
        RateUs = 11,
        RateUsV2 = 12,
        Privacy = 13,
        PreAttGuide = 14,
        PreAttGuideV2 = 15,
        PrePushGuide = 16,
        Confirm = 17,
        Win = 18,
        DailyWin = 19,
        Fail = 20,
        DailyFail = 21,
        AdRewardRestored = 22,
        Award = 23,
        Streak = 24,
        StreakResume = 25,
        StreakBackfill = 26,
        AbSwitchPopup = 27,
        RankActivityOpenPopup = 28,
        RankActivityHowToPlay = 29,
        RankActivityPage = 30,
        RankActivityChange = 31,
        Profile = 32,
        Debug = 33,
        Generator = 34,
        AbDebug = 35,
        LevelJsonInput = 36,
        MockAd = 37,
        MockBanner = 38
    }

    public enum UiLayer
    {
        Default = 0,
        Popup = 100,
        Notice = 200,
        Modal = 300,
        Tutorial = 400,
        Loading = 500
    }

    public enum UiWindowState
    {
        Invalid = 0,
        Creating = 1,
        Showing = 2,
        Hidden = 3,
        Closing = 4,
        Destroyed = 5
    }

    public static class UiLayerConfig
    {
        public const int ZStep = 50;
        public const int ZMax = 4000;
    }

    /// <summary>
    /// Pure startup timing and routing rules ported from launcher.gd.
    /// This belongs to the shared UI contract assembly surface so EditMode
    /// fixtures do not depend on the scene-owned AppBootstrap component.
    /// </summary>
    public static class AppStartupContract
    {
        public const float ExternalWaitMaximumSeconds = 2f;
        public const float MinimumSplashSeconds = 2f;
        public const float SplashCompletionPaddingSeconds = 0.5f;

        public static float SplashWaitRemaining(float elapsedSeconds)
        {
            return elapsedSeconds >= MinimumSplashSeconds
                ? SplashCompletionPaddingSeconds
                : MinimumSplashSeconds - Math.Max(0f, elapsedSeconds) +
                  SplashCompletionPaddingSeconds;
        }

        public static UiName InitialRoute(bool tutorialDone)
        {
            return tutorialDone ? UiName.Home : UiName.Tutorial;
        }
    }

    public sealed class UIEvents
    {
        public event Action<UiName, UIFrameWindow> WindowCreated;
        public event Action<UiName, UIFrameWindow> WindowShown;
        public event Action<UiName, UIFrameWindow> WindowHidden;

        internal void RaiseCreated(UiName name, UIFrameWindow window) =>
            WindowCreated?.Invoke(name, window);

        internal void RaiseShown(UiName name, UIFrameWindow window) =>
            WindowShown?.Invoke(name, window);

        internal void RaiseHidden(UiName name, UIFrameWindow window) =>
            WindowHidden?.Invoke(name, window);

        internal void Clear()
        {
            WindowCreated = null;
            WindowShown = null;
            WindowHidden = null;
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Gameplay/SettingsPagePresenter.cs
``csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using Meowdoku.Core.Localization;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SettingsPagePresenter : UIFrameWindow,
        IAbConfigRuntimeConsumer,
        ISettingsExternalServicesConsumer
    {
        public override string GetTrackingDialogName() => _isGameMode
            ? TrackerCatalog.Dialog.Options
            : TrackerCatalog.Dialog.Settings;

        [Header("Popup hierarchy")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private GenericPopupAnimator popupAnimator;
        [SerializeField] private Text titleText;
        [SerializeField] private LocalizedText versionLocalizedText;

        [Header("Toggle grid")]
        [SerializeField] private RectTransform toggleGrid;
        [SerializeField] private HorizontalLayoutGroup toggleGridLayout;
        [SerializeField] private SettingsToggleView musicToggle;
        [SerializeField] private SettingsToggleView soundToggle;
        [SerializeField] private SettingsToggleView vibrationToggle;
        [SerializeField] private SettingsToggleView peopleToggle;

        [Header("Optional switch rows")]
        [SerializeField] private GameObject optionalSwitchSpacer;
        [SerializeField] private GameObject optionalSwitchContainer;
        [SerializeField] private LanguageSwitchWidget languageSwitchWidget;
        [SerializeField] private GameObject patternSwitch;
        [SerializeField] private Button patternButton;
        [SerializeField] private GameObject patternOn;
        [SerializeField] private GameObject patternOff;
        [SerializeField] private GameObject patternDot;

        [Header("Action rows")]
        [SerializeField] private GameObject actionSpacer;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private GameObject restartRow;
        [SerializeField] private Button restartButton;
        [SerializeField] private LayoutElement afterActionsSpacer;
        [SerializeField] private GameObject cmpRow;
        [SerializeField] private Button cmpButton;
        [SerializeField] private GameObject termsRow;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private GameObject versionRow;
        [SerializeField] private Text versionText;
        [SerializeField] private LayoutElement bottomSpacer;

        [Header("Feedback boundaries")]
        [SerializeField] private SourceToastView toast;
        [SerializeField] private LocalizationCatalog localization;
        [SerializeField] private SoundService soundService;
        [SerializeField] private bool cmpRequired;

        private readonly SettingsLanguageConfig _languageConfig = new();
        private readonly BlindModConfig _blindModeConfig = new();
        private readonly RuleTextConfig _ruleTextConfig = new();
        private AbConfigRuntime _abConfigRuntime;
        private ISettingsExternalServices _externalServices =
            OfflineSettingsExternalServices.Instance;

        private Action _onRestart;
        private Action _onPatternChanged;
        private Action _onClose;
        private Action _onFeedback;
        private Action _onCmp;
        private Action _onVibrationPreview;
        private bool _isGameMode;
        private bool _restartConsumed;
        private bool _skipNextCloseAnimation;
        private bool _suppressNextCloseCallback;
        private bool _waitingForHowToPlay;
        private HowToPlayPagedPagePresenter _howToPlayPage;
#if UNITY_INCLUDE_TESTS
        private string _systemLocaleOverrideForTests = string.Empty;
#endif

        public bool IsGameMode => _isGameMode;

        protected override void OnCreate()
        {
            BindToggle(musicToggle, ToggleMusic);
            BindToggle(soundToggle, ToggleSound);
            BindToggle(vibrationToggle, ToggleVibration);
            BindToggle(peopleToggle, TogglePeople);
            AddListener(patternButton, TogglePattern);
            AddListener(languageButton, OpenLanguage);
            AddListener(feedbackButton, OpenFeedback);
            AddListener(howToPlayButton, OpenHowToPlay);
            AddListener(restartButton, RestartGame);
            AddListener(cmpButton, OpenCmp);
            AddListener(termsButton, OpenTerms);
            AddListener(privacyButton, OpenPrivacy);
            if (languageSwitchWidget != null)
            {
                languageSwitchWidget.LanguagePicked += ApplyLanguageAndClose;
                languageSwitchWidget.DropdownOpened += HandleLanguageDropdownOpened;
                languageSwitchWidget.DropdownClosed += HandleLanguageDropdownClosed;
            }
            if (localization != null)
                localization.LocaleChanged += RefreshStaticText;
            RefreshStaticText();
        }

        protected override void OnShow(
            IReadOnlyDictionary<string, object> parameters)
        {
            popupAnimator?.Stop();
            DetachHowToPlayWait();
            _isGameMode = Parameter(parameters, "is_game_mode", false);
            _onRestart = Parameter<Action>(parameters, "on_restart");
            _onPatternChanged = Parameter<Action>(parameters, "on_pattern_changed");
            _onClose = Parameter<Action>(parameters, "on_close");
            _onFeedback = Parameter<Action>(parameters, "on_feedback");
            _onCmp = Parameter<Action>(parameters, "on_cmp");
            _onVibrationPreview = Parameter<Action>(parameters, "on_vibration_preview");
            _restartConsumed = false;
            _skipNextCloseAnimation = false;
            _suppressNextCloseCallback = false;

            _abConfigRuntime?.ReloadTiming(AbConfigTiming.OpenSetting);
            SettingsLanguageConfig languageConfig = LanguageConfig;
            string systemLocale = ResolveSystemLocale();
            localization?.ApplySystemLocale(
                GameStateRuntime.Current,
                languageConfig.IsLanguageSwitchEnabled(),
                systemLocale);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                _isGameMode,
                systemLocale,
                GameStateRuntime.Current.TutorialDone,
                GameStateRuntime.Current.PatternSwitchDotDismissed,
                cmpRequired || _externalServices.IsConsentManagementRequired,
                languageConfig,
                BlindModeConfig,
                RuleTextConfig);
            ApplyLayout(state);
            if (state.ShowLanguageDropdown)
            {
                languageSwitchWidget?.Setup(
                    systemLocale);
            }
            RefreshToggleValues();
            RefreshStaticText();
            RebuildAndCenterPanel();
            popupAnimator?.PlayOpen();
            soundService?.Play(SoundKind.DialogOpen);
        }

        protected override IEnumerator PlayCloseAnimation()
        {
            if (_skipNextCloseAnimation)
            {
                _skipNextCloseAnimation = false;
                yield break;
            }

            if (popupAnimator != null)
                yield return popupAnimator.PlayClose();
        }

        protected override IEnumerator OnHide()
        {
            popupAnimator?.Stop();
            languageSwitchWidget?.ForceClose();
            bool shouldCall = !_suppressNextCloseCallback;
            _suppressNextCloseCallback = false;
            if (shouldCall) _onClose?.Invoke();
            yield break;
        }

        protected override void OnCloseButtonPressed()
        {
            TrackButton(TrackerCatalog.Button.Close);
        }

        protected override void OnDestroyWindow()
        {
            popupAnimator?.Stop();
            DetachHowToPlayWait();
            UnbindToggle(musicToggle, ToggleMusic);
            UnbindToggle(soundToggle, ToggleSound);
            UnbindToggle(vibrationToggle, ToggleVibration);
            UnbindToggle(peopleToggle, TogglePeople);
            RemoveListener(patternButton, TogglePattern);
            RemoveListener(languageButton, OpenLanguage);
            RemoveListener(feedbackButton, OpenFeedback);
            RemoveListener(howToPlayButton, OpenHowToPlay);
            RemoveListener(restartButton, RestartGame);
            RemoveListener(cmpButton, OpenCmp);
            RemoveListener(termsButton, OpenTerms);
            RemoveListener(privacyButton, OpenPrivacy);
            if (languageSwitchWidget != null)
            {
                languageSwitchWidget.LanguagePicked -= ApplyLanguageAndClose;
                languageSwitchWidget.DropdownOpened -= HandleLanguageDropdownOpened;
                languageSwitchWidget.DropdownClosed -= HandleLanguageDropdownClosed;
            }
            if (localization != null)
                localization.LocaleChanged -= RefreshStaticText;
            base.OnDestroyWindow();
        }

        public void BindSoundService(SoundService service)
        {
            soundService = service;
        }

        public void BindLocalization(LocalizationCatalog catalog)
        {
            if (localization == catalog) return;
            if (localization != null)
                localization.LocaleChanged -= RefreshStaticText;
            localization = catalog;
            if (localization != null)
                localization.LocaleChanged += RefreshStaticText;
            RefreshStaticText();
        }

        public void BindAbConfigRuntime(AbConfigRuntime runtime)
        {
            _abConfigRuntime = runtime;
        }

        public void BindSettingsExternalServices(
            ISettingsExternalServices services)
        {
            _externalServices = services ??
                OfflineSettingsExternalServices.Instance;
        }

#if UNITY_INCLUDE_TESTS
        internal void OverrideSystemLocaleForTests(string locale)
        {
            _systemLocaleOverrideForTests = locale ?? string.Empty;
        }
#endif

        private SettingsLanguageConfig LanguageConfig =>
            _abConfigRuntime?.Settings.Language ?? _languageConfig;

        private BlindModConfig BlindModeConfig =>
            _abConfigRuntime?.Settings.BlindMode ?? _blindModeConfig;

        private RuleTextConfig RuleTextConfig =>
            _abConfigRuntime?.Settings.RuleText ?? _ruleTextConfig;

        private void ApplyLayout(SettingsPresentationState state)
        {
            SetActive(musicToggle, state.ShowMusic);
            SetActive(soundToggle, state.ShowSound);
            SetActive(vibrationToggle, state.ShowVibration);
            SetActive(peopleToggle, state.ShowPeople);
            if (toggleGridLayout != null)
                toggleGridLayout.spacing = state.ToggleHorizontalSeparation;

            if (languageSwitchWidget != null)
                languageSwitchWidget.gameObject.SetActive(
                    state.ShowLanguageDropdown);
            if (patternSwitch != null)
                patternSwitch.SetActive(state.ShowPattern);
            if (optionalSwitchContainer != null)
                optionalSwitchContainer.SetActive(state.ShowToggleContainer);
            if (optionalSwitchSpacer != null)
                optionalSwitchSpacer.SetActive(state.ShowToggleContainer);
            if (patternDot != null) patternDot.SetActive(state.ShowPatternDot);

            SetActive(languageButton, state.ShowLanguageButton);
            SetActive(feedbackButton, state.ShowFeedback);
            SetActive(howToPlayButton, state.ShowHowToPlay);
            if (restartRow != null) restartRow.SetActive(state.ShowRestart);
            if (actionSpacer != null) actionSpacer.SetActive(true);
            if (afterActionsSpacer != null)
                afterActionsSpacer.preferredHeight = state.IsGameMode ? 0f : 50f;
            if (cmpRow != null) cmpRow.SetActive(state.ShowCmp);
            if (termsRow != null) termsRow.SetActive(state.ShowTerms);
            if (versionRow != null) versionRow.SetActive(state.ShowVersion);
            if (bottomSpacer != null)
                bottomSpacer.preferredHeight = state.BottomSpacerMinimum;
        }

        private void RefreshToggleValues()
        {
            GameStateService state = GameStateRuntime.Current;
            musicToggle?.SetValue(state.MusicOn);
            soundToggle?.SetValue(state.SoundOn);
            vibrationToggle?.SetValue(state.VibrationOn);
            peopleToggle?.SetValue(state.PeopleOn);
            SetPatternVisual(state.PatternModeOn);
        }

        private void RefreshStaticText()
        {
            if (titleText != null)
                titleText.text = Translate("SETTING_TITLE", "Settings");
            if (versionText != null)
            {
                if (versionLocalizedText != null)
                    versionLocalizedText.SetArguments(Application.version);
                else
                {
                    string format = Translate("SETTING_VERSION", "Version %s");
                    versionText.text = format.Replace("%s", Application.version);
                }
            }
        }

        private void RebuildAndCenterPanel()
        {
            if (panel == null) return;
            Canvas.ForceUpdateCanvases();
            if (panel.childCount > 0 &&
                panel.GetChild(0) is RectTransform layoutRoot)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
                float preferredHeight =
                    LayoutUtility.GetPreferredHeight(layoutRoot);
                if (preferredHeight > 0f)
                    panel.SetSizeWithCurrentAnchors(
                        RectTransform.Axis.Vertical,
                        preferredHeight);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
        }

        private void ToggleMusic()
        {
            bool value = !GameStateRuntime.Current.MusicOn;
            GameStateRuntime.Current.SetMusicOn(value);
            soundService?.RefreshBgm();
            musicToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_MUSIC_ON" : "SETTING_MUSIC_OFF",
                value ? "Music On" : "Music Off");
            TrackSwitch(TrackerCatalog.Switch.Music, value);
        }

        private void ToggleSound()
        {
            bool value = !GameStateRuntime.Current.SoundOn;
            GameStateRuntime.Current.SetSoundOn(value);
            soundToggle?.SetValue(value);
            if (value) soundService?.Play(SoundKind.ButtonClick);
            ShowToast(
                value ? "SETTING_SOUND_ON" : "SETTING_SOUND_OFF",
                value ? "Sound On" : "Sound Off");
            TrackSwitch(TrackerCatalog.Switch.Sound, value);
        }

        private void ToggleVibration()
        {
            bool value = !GameStateRuntime.Current.VibrationOn;
            GameStateRuntime.Current.SetVibrationOn(value);
            vibrationToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_VIBRATION_ON" : "SETTING_VIBRATION_OFF",
                value ? "Vibration On" : "Vibration Off");
            if (value)
            {
                if (_onVibrationPreview != null)
                    _onVibrationPreview.Invoke();
                else if (Application.isMobilePlatform)
                    Handheld.Vibrate();
            }
            TrackSwitch(TrackerCatalog.Switch.Vibration, value);
        }

        private void TogglePeople()
        {
            bool value = !GameStateRuntime.Current.PeopleOn;
            GameStateRuntime.Current.SetPeopleOn(value);
            peopleToggle?.SetValue(value);
            ShowToast(
                value ? "SETTING_PEOPLE_ON" : "SETTING_PEOPLE_OFF",
                value ? "Voice On" : "Voice Off");
        }

        private void TogglePattern()
        {
            bool value = !GameStateRuntime.Current.PatternModeOn;
            GameStateRuntime.Current.SetPatternModeOn(value);
            SetPatternVisual(value);
            if (!GameStateRuntime.Current.PatternSwitchDotDismissed)
            {
                GameStateRuntime.Current.MarkPatternSwitchDotDismissed();
                if (patternDot != null) patternDot.SetActive(false);
            }
            ShowToast(
                value ? "SETTING_PATTERN_ON" : "SETTING_PATTERN_OFF",
                value ? "Pattern Mode On" : "Pattern Mode Off");
            Tracking?.TrackSwitchClick(
                TrackerCatalog.Switch.Pattern,
                value ? 1 : 0,
                TrackerCatalog.Dialog.Options);
            _onPatternChanged?.Invoke();
        }

        private void SetPatternVisual(bool value)
        {
            if (patternOn != null) patternOn.SetActive(value);
            if (patternOff != null) patternOff.SetActive(!value);
        }

        private void RestartGame()
        {
            if (_restartConsumed) return;
            _restartConsumed = true;
            TrackButton(TrackerCatalog.Button.Restart);
            _onRestart?.Invoke();
            Owner?.Hide(UiName.Setting);
        }

        private void OpenLanguage()
        {
            TrackButton(TrackerCatalog.Button.Language);
            Owner?.Show(UiName.Language);
        }

        private void ApplyLanguageAndClose(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return;
            TrackButton(TrackerCatalog.Button.LanguageConfirm);
            Tracking?.TrackUiLanguage(locale);
            localization?.SetLocale(locale);
            GameStateRuntime.Current.SetAppliedLocale(locale);
            Owner?.Hide(UiName.Setting);
        }

        private void OpenHowToPlay()
        {
            if (Owner == null || _waitingForHowToPlay) return;
            UIFrameWindow page = Owner.Show(UiName.HowToPlayPaged);
            HowToPlayPagedPagePresenter paged =
                page as HowToPlayPagedPagePresenter;
            if (paged == null) return;
            _waitingForHowToPlay = true;
            _howToPlayPage = paged;
            _howToPlayPage.Closed += HandleHowToPlayClosed;
            Owner.Events.WindowHidden += HandleWindowHidden;
            _skipNextCloseAnimation = true;
            _suppressNextCloseCallback = true;
            Owner.Hide(UiName.Setting);
        }

        private void HandleHowToPlayClosed()
        {
            if (!_waitingForHowToPlay) return;
            Action callback = _onClose;
            DetachHowToPlayWait();
            callback?.Invoke();
        }

        private void HandleWindowHidden(UiName name, UIFrameWindow _)
        {
            if (!_waitingForHowToPlay || name != UiName.HowToPlayPaged) return;
            DetachHowToPlayWait();
        }

        private void DetachHowToPlayWait()
        {
            if (_howToPlayPage != null)
                _howToPlayPage.Closed -= HandleHowToPlayClosed;
            if (_waitingForHowToPlay && Owner != null)
                Owner.Events.WindowHidden -= HandleWindowHidden;
            _waitingForHowToPlay = false;
            _howToPlayPage = null;
        }

        private void OpenFeedback()
        {
            TrackButton(TrackerCatalog.Button.Feedback);
            bool online = _onFeedback != null
                ? Application.internetReachability !=
                  NetworkReachability.NotReachable
                : _externalServices.IsOnline;
            if (!online)
            {
                ShowToast("NETWORK_ERROR", "Please check your network connection.");
                return;
            }
            if (_onFeedback != null) _onFeedback.Invoke();
            else _externalServices.OpenFeedbackFaq();
        }

        private void OpenCmp()
        {
            TrackButton(TrackerCatalog.Button.PrivacyPreference);
            if (_onCmp != null) _onCmp.Invoke();
            else _externalServices.ShowConsentManagement();
        }

        private void OpenTerms()
        {
            TrackButton(TrackerCatalog.Button.Terms);
            _externalServices.OpenLocalizedPrivacyUrl(
                "https://oakevergames.com/tos.html");
        }

        private void OpenPrivacy()
        {
            TrackButton(TrackerCatalog.Button.Privacy);
            _externalServices.OpenLocalizedPrivacyUrl(
                "https://oakevergames.com/pp.html");
        }

        private void HandleLanguageDropdownOpened()
        {
            TrackButton(TrackerCatalog.Button.Language);
            Tracking?.TrackDialogShown(
                TrackerCatalog.Dialog.LanguagePicker);
        }

        private void HandleLanguageDropdownClosed()
        {
            Tracking?.NotifyDialogClosed(
                TrackerCatalog.Dialog.LanguagePicker);
        }

        private void TrackButton(string name)
        {
            Tracking?.TrackButtonClick(name, GetTrackingDialogName());
        }

        private void TrackSwitch(string name, bool value)
        {
            Tracking?.TrackSwitchClick(
                name,
                value ? 1 : 0,
                GetTrackingDialogName());
        }

        private void ShowToast(string key, string fallback)
        {
            toast?.Show(Translate(key, fallback));
        }

        private string Translate(string key, string fallback)
        {
            if (localization == null) return fallback;
            string value = localization.Translate(key);
            return string.Equals(value, key, StringComparison.Ordinal)
                ? fallback
                : value;
        }

        private string ResolveSystemLocale()
        {
#if UNITY_INCLUDE_TESTS
            if (!string.IsNullOrWhiteSpace(_systemLocaleOverrideForTests))
                return LocalizationLocaleContract.NormalizeLocale(
                    _systemLocaleOverrideForTests);
#endif
            return LocalizationLocaleContract.ResolveCurrentSystemLocale();
        }

        private static void BindToggle(
            SettingsToggleView view,
            UnityEngine.Events.UnityAction action)
        {
            if (view?.Button != null) view.Button.onClick.AddListener(action);
        }

        private static void UnbindToggle(
            SettingsToggleView view,
            UnityEngine.Events.UnityAction action)
        {
            if (view?.Button != null) view.Button.onClick.RemoveListener(action);
        }

        private static void AddListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        private static void RemoveListener(
            Button button,
            UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.RemoveListener(action);
        }

        private static void SetActive(Component component, bool active)
        {
            if (component != null) component.gameObject.SetActive(active);
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }

        private static T Parameter<T>(
            IReadOnlyDictionary<string, object> parameters,
            string key) where T : class
        {
            return parameters != null && parameters.TryGetValue(key, out object raw)
                ? raw as T
                : null;
        }

        private static bool Parameter(
            IReadOnlyDictionary<string, object> parameters,
            string key,
            bool fallback)
        {
            return parameters != null && parameters.TryGetValue(key, out object raw) &&
                   raw is bool value
                ? value
                : fallback;
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/UI/SettingsExternalServices.cs
``csharp
using UnityEngine;

namespace Meowdoku.Core.UI
{
    /// <summary>
    /// Provider-neutral boundary for Settings actions owned by platform SDKs.
    /// A production adapter may implement this together with
    /// IAppStartupExternalServices; the offline fallback never blocks UI.
    /// </summary>
    public interface ISettingsExternalServices
    {
        bool IsOnline { get; }
        bool IsConsentManagementRequired { get; }
        void OpenFeedbackFaq();
        void ShowConsentManagement();
        void OpenLocalizedPrivacyUrl(string defaultUrl);
    }

    public interface ISettingsExternalServicesConsumer
    {
        void BindSettingsExternalServices(ISettingsExternalServices services);
    }

    public sealed class OfflineSettingsExternalServices :
        ISettingsExternalServices
    {
        public static readonly OfflineSettingsExternalServices Instance = new();

        private OfflineSettingsExternalServices() { }

        public bool IsOnline => false;
        public bool IsConsentManagementRequired => false;
        public void OpenFeedbackFaq() { }
        public void ShowConsentManagement() { }

        public void OpenLocalizedPrivacyUrl(string defaultUrl)
        {
            if (!string.IsNullOrWhiteSpace(defaultUrl))
                Application.OpenURL(defaultUrl);
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/Tracking/TrackerService.cs
``csharp
using System;
using System.Collections.Generic;

namespace Meowdoku.Core.Tracking
{
    public interface ITrackingSink
    {
        void SendEvent(
            string eventName,
            IReadOnlyDictionary<string, object> parameters);
        void SetUserProperty(string name, string value);
    }

    public sealed class NullTrackingSink : ITrackingSink
    {
        public static readonly NullTrackingSink Instance = new();
        private NullTrackingSink() { }
        public void SendEvent(
            string eventName,
            IReadOnlyDictionary<string, object> parameters) { }
        public void SetUserProperty(string name, string value) { }
    }

    public interface ITrackingIdProvider
    {
        string NewId();
    }

    public sealed class GuidTrackingIdProvider : ITrackingIdProvider
    {
        public static readonly GuidTrackingIdProvider Instance = new();
        private GuidTrackingIdProvider() { }
        public string NewId() => Guid.NewGuid().ToString("D");
    }

    public static class TrackerCatalog
    {
        public static class Event
        {
            public const string ScreenShow = "scr_show";
            public const string DialogShow = "dlg_show";
            public const string ButtonClick = "btn_click";
            public const string GameStart = "game_start";
            public const string GameEnd = "game_end";
            public const string PropGet = "prop_get";
            public const string PropUse = "prop_use";
            public const string AdShowTiming = "ad_show_timing";
            public const string InterstitialAdShow =
                "interstitial_ad_show";
            public const string RewardedAdShow = "rewarded_ad_show";
            public const string SwitchClick = "sw_click";
            public const string NewGuideShow = "new_guide_show";
            public const string NewGuideEnd = "new_guide_end";
            public const string NewGuideStep = "new_guide_step";
            public const string PerfMonitor = "perf_monitor";
            public const string RemoveAppStart = "remove_app_start";
            public const string SparkStreak = "spark_streak";
            public const string PushGuideResult = "push_guide_result";
            public const string RankStart = "rank_start";
            public const string RankData = "rank_data";
            public const string FrameGet = "frame_get";
        }

        public static class Screen
        {
            public const string Splash = "splash_scr";
            public const string Home = "homepage_scr";
            public const string NormalGame = "normal_game_scr";
            public const string NormalWin = "normal_game_success_scr";
            public const string NormalFail = "normal_game_fail_scr";
            public const string DailyGame = "daily_game_scr";
            public const string DailyWin = "daily_game_success_scr";
            public const string DailyFail = "daily_game_fail_scr";
            public const string Feedback = "feedback_scr";
            public const string Streak = "streak_scr";
            public const string GameStreak = "game_streak_scr";
            public const string ChallengeRank = "challenge_rank_scr";
        }

        public static class Dialog
        {
            public const string Privacy = "privacy_dlg";
            public const string PreAttGuide = "pre_att_guide_dlg";
            public const string Rate = "rate_dlg";
            public const string Feedback = "feedback_dlg";
            public const string Settings = "settings_dlg";
            public const string Options = "options_dlg";
            public const string NormalToast = "game_normal_toast_dlg";
            public const string HardToast = "game_hard_toast_dlg";
            public const string RewardFail = "reward_fail_dlg";
            public const string LanguagePicker = "language_picker_dlg";
            public const string PushGuide = "push_guide_dlg";
            public const string ChallengeGuide = "challenge_guide_dlg";
            public const string Avatar = "avatar_dlg";
            public const string ChallengeRank = "challenge_rank_dlg";
            public const string ChallengeReward =
                "challenge_reward_dlg";
            public const string ChallengeRewardGet =
                "challenge_reward_get_dlg";
        }

        public static class GameType
        {
            public const string Normal = "normal";
            public const string Daily = "daily";
        }

        public static class GameStatus
        {
            public const string New = "new";
            public const string Continue = "continue";
            public const string Restart = "restart";
        }

        public static class GameResult
        {
            public const string Win = "win";
            public const string Fail = "fail";
            public const string Quit = "quit";
        }

        public static class Prop
        {
            public const string Hint = "hint";
            public const string Locate = "locate";
            public const string Undo = "undo";
        }

        public static class PropSource
        {
            public const string HintRewardAd = "hint_reward_ad";
            public const string LocateRewardAd = "locate_reward_ad";
            public const string UndoRewardAd = "undo_reward_ad";
            public const string RewardFailDialog = "reward_fail_dlg";
            public const string StreakChest = "streak_chest";
            public const string StreakRewardAd = "streak_reward_ad";
            public const string SwitchGroup = "switch_group";
            public const string ChallengeGetDialog = "challenge_get_dlg";
            public const string ChallengeRewardGetDialog =
                "challenge_reward_get_dlg";
        }

        public static class Placement
        {
            public const string Interstitial = "interstitial";
            public const string Reward = "reward";
            public const string Banner = "banner";
            public const string AppOpen = "appopen";
        }

        public static class AdPosition
        {
            public const string NormalGameFail = "normal_game_fail";
            public const string DailyGameFail = "daily_game_fail";
            public const string PropsNormalHint = "props_normal_hint";
            public const string PropsNormalLocate = "props_normal_locate";
            public const string PropsDailyHint = "props_daily_hint";
            public const string PropsDailyLocate = "props_daily_locate";
            public const string StreakDoubleReward = "streak_x2_reward";
            public const string StreakReviveReward = "streak_revive_reward";
            public const string RankReward = "rank_reward";
            public const string NormalStart = "normal_start";
            public const string NormalSuccess = "normal_success";
            public const string NormalRestart = "normal_restart";
            public const string NormalContinue = "normal_continue";
        }

        public static class Button
        {
            public const string NormalPlay = "normal_play";
            public const string DailyPlay = "daily_play";
            public const string Settings = "settings";
            public const string Streak = "streak";
            public const string GoToPlay = "gotoplay";
            public const string Back = "back";
            public const string Hint = "hint";
            public const string Locate = "locate";
            public const string Clear = "clear";
            public const string Coordinate = "coord";
            public const string HintApply = "hint_apply";
            public const string HintStop = "hint_stop";
            public const string HintDetail = "hint_detail";
            public const string Options = "options";
            public const string LevelPlay = "level_play";
            public const string Revive = "revive";
            public const string Restart = "restart";
            public const string TryAgain = "try_again";
            public const string Continue = "continue";
            public const string Close = "close";
            public const string Feedback = "feedback";
            public const string Terms = "terms";
            public const string Policy = "policy";
            public const string Privacy = "privacy";
            public const string PrivacyPreference = "privacy_preference";
            public const string Language = "language";
            public const string LanguageConfirm = "language_confirm";
            public const string LanguageCancel = "language_cancel";
            public const string Submit = "submit";
            public const string FeedbackRecord = "feedback_record";
            public const string Accept = "accept";
            public const string AttContinue = "att_continue";
            public const string RateUs = "rate_us";
            public const string Collect = "collect";
            public const string PushGuideJoin = "push_guide_join";
            public const string PushGuideClose = "push_guide_close";
            public const string Play = "play";
            public const string Save = "save";
            public const string CollectDouble = "collect_double";
            public const string ChallengeInfo = "challenge_info";
            public const string SelfInfo = "self_info";
            public const string ChallengeEntrance = "challenge_entrance";
        }

        public static class Switch
        {
            public const string Music = "music_sw";
            public const string Sound = "sound_sw";
            public const string Vibration = "vibration_sw";
            public const string Pattern = "pattern_sw";
        }

        public static class UserProperty
        {
            public const string UiLanguage = "ui_language";
        }
    }

    public sealed class TrackerService
    {
        private static readonly string[] RoundStatKeys =
        {
            "hint_used", "locate_used", "hint_apply_used",
            "hint_stop_used", "hint_detail_used", "clear_used",
            "step_used", "erase_count", "hint_cross_count"
        };

        private readonly GameStateService _gameState;
        private readonly ITrackingSink _sink;
        private readonly ITrackingIdProvider _ids;
        private readonly Dictionary<string, object> _mainStats = new();
        private readonly Dictionary<string, object> _dailyStats = new();
        private readonly List<string> _sourceStack = new();
        private readonly Dictionary<string, string> _pendingAdIds = new();
        private string _currentGameId = string.Empty;
        private string _activeGameType = string.Empty;

        public TrackerService(
            GameStateService gameState,
            ITrackingSink sink = null,
            ITrackingIdProvider ids = null)
        {
            _gameState = gameState ??
                         throw new ArgumentNullException(nameof(gameState));
            _sink = sink ?? NullTrackingSink.Instance;
            _ids = ids ?? GuidTrackingIdProvider.Instance;
        }

        public string CurrentGameId => _currentGameId;
        public string CurrentSource => _sourceStack.Count > 0
            ? _sourceStack[_sourceStack.Count - 1]
            : string.Empty;

        public void SetActiveGameType(string gameType)
        {
            _activeGameType = gameType ?? string.Empty;
            _currentGameId =
                _gameState.GetPersistedGameId(_activeGameType);
            Dictionary<string, object> active = ActiveStats();
            if (active.Count != 0) return;
            Dictionary<string, object> persisted =
                _gameState.GetGameRoundStats(_activeGameType);
            foreach (KeyValuePair<string, object> pair in persisted)
                active[pair.Key] = pair.Value;
        }

        public string NewGameId(string gameType)
        {
            _activeGameType = gameType ?? string.Empty;
            _currentGameId = _ids.NewId();
            ActiveStats().Clear();
            _gameState.ResetGameTotalStats(_activeGameType);
            _gameState.ResetGameRoundStats(_activeGameType);
            _gameState.SetPersistedGameId(
                _activeGameType,
                _currentGameId);
            return _currentGameId;
        }

        public void IncrementStat(string key, int delta = 1)
        {
            if (string.IsNullOrEmpty(key)) return;
            Dictionary<string, object> stats = ActiveStats();
            stats[key] = ReadInt(stats, key) + delta;
            _gameState.PersistGameRoundStats(_activeGameType, stats);
        }

        public int GetStat(string key) => string.IsNullOrEmpty(key)
            ? 0
            : ReadInt(ActiveStats(), key);

        public void ResetRoundStats()
        {
            Dictionary<string, object> stats = ActiveStats();
            for (int index = 0; index < RoundStatKeys.Length; index++)
                stats.Remove(RoundStatKeys[index]);
            _gameState.PersistGameRoundStats(_activeGameType, stats);
        }

        public void OnRestart()
        {
            ResetRoundStats();
            IncrementStat("restart_count");
        }

        public void NotifyDialogClosed(string dialogName)
        {
            if (string.IsNullOrEmpty(dialogName)) return;
            int index = _sourceStack.LastIndexOf(dialogName);
            if (index < 0) return;
            _sourceStack.RemoveRange(
                index,
                _sourceStack.Count - index);
        }

        public void TrackScreenShown(string screenName, string source = "")
        {
            if (string.IsNullOrEmpty(screenName)) return;
            string previous = string.IsNullOrEmpty(source)
                ? CurrentSource
                : source;
            var parameters = new Dictionary<string, object>
            {
                ["scr_name"] = screenName
            };
            AddSource(parameters, previous);
            Send(TrackerCatalog.Event.ScreenShow, parameters);
            _sourceStack.Clear();
            _sourceStack.Add(screenName);
        }

        public void TrackDialogShown(
            string dialogName,
            string source = "",
            IReadOnlyDictionary<string, object> extra = null)
        {
            if (string.IsNullOrEmpty(dialogName)) return;
            string previous = string.IsNullOrEmpty(source)
                ? CurrentSource
                : source;
            var parameters = new Dictionary<string, object>
            {
                ["dlg_name"] = dialogName
            };
            AddSource(parameters, previous);
            Merge(parameters, extra);
            Send(TrackerCatalog.Event.DialogShow, parameters);
            _sourceStack.Add(dialogName);
        }

        public void TrackButtonClick(
            string buttonName,
            string source = "",
            IReadOnlyDictionary<string, object> extra = null)
        {
            if (string.IsNullOrEmpty(buttonName)) return;
            var parameters = new Dictionary<string, object>
            {
                ["btn_name"] = buttonName
            };
            AddSource(
                parameters,
                string.IsNullOrEmpty(source) ? CurrentSource : source);
            Merge(parameters, extra);
            Send(TrackerCatalog.Event.ButtonClick, parameters);
        }

        public void TrackGameStart(
            string qid,
            string qrotate,
            string status,
            string gameType,
            int difficulty,
            int level,
            int strategyLayer,
            int scale,
            int isChallenge,
            string preType = "0")
        {
            Send(TrackerCatalog.Event.GameStart,
                new Dictionary<string, object>
                {
                    ["qid"] = qid,
                    ["qrotate"] = qrotate,
                    ["status"] = status,
                    ["game_type"] = gameType,
                    ["diffi"] = difficulty,
                    ["level"] = level,
                    ["strategy_layer"] = strategyLayer,
                    ["is_challenge"] = isChallenge,
                    ["scale"] = scale,
                    ["pre_type"] = preType
                });
        }

        public void TrackGameEnd(
            IReadOnlyDictionary<string, object> values) =>
            Send(TrackerCatalog.Event.GameEnd, Copy(values));

        public void TrackProp(
            bool acquired,
            string propName,
            string source,
            int propNum,
            int propLeft)
        {
            Send(
                acquired
                    ? TrackerCatalog.Event.PropGet
                    : TrackerCatalog.Event.PropUse,
                new Dictionary<string, object>
                {
                    ["prop_name"] = propName,
                    ["source"] = source,
                    ["prop_num"] = propNum,
                    ["prop_left"] = propLeft
                });
        }

        public string GenerateAdShowId() => _ids.NewId();

        public void RememberAdShowId(string placementType, string id)
        {
            if (!string.IsNullOrEmpty(placementType))
                _pendingAdIds[placementType] = id ?? string.Empty;
        }

        public string ConsumeAdShowId(string placementType)
        {
            if (string.IsNullOrEmpty(placementType) ||
                !_pendingAdIds.TryGetValue(placementType, out string id))
                return string.Empty;
            _pendingAdIds.Remove(placementType);
            return id;
        }

        public void TrackAdShowTiming(
            string adShowId,
            string placement,
            string placementType,
            string position)
        {
            Send(TrackerCatalog.Event.AdShowTiming,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["placement"] = placement ?? string.Empty,
                    ["placement_type"] = placementType ?? string.Empty,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackInterstitialAdShow(
            string adShowId,
            int level,
            string position)
        {
            Send(TrackerCatalog.Event.InterstitialAdShow,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["level"] = level,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackRewardedAdShow(
            string adShowId,
            int level,
            string position)
        {
            Send(TrackerCatalog.Event.RewardedAdShow,
                new Dictionary<string, object>
                {
                    ["ad_show_id"] = adShowId ?? string.Empty,
                    ["level"] = level,
                    ["position"] = position ?? string.Empty
                });
        }

        public void TrackSwitchClick(
            string switchName,
            int state,
            string source) =>
            Send(TrackerCatalog.Event.SwitchClick,
                new Dictionary<string, object>
                {
                    ["sw_name"] = switchName,
                    ["state"] = state,
                    ["source"] = source
                });

        public void TrackRankStart(int rankId) =>
            Send(TrackerCatalog.Event.RankStart,
                new Dictionary<string, object> { ["rank_id"] = rankId });

        public void TrackRankData(
            int rankId,
            string source,
            int rank,
            int normalNum,
            string nickname,
            int avatar,
            int frameId,
            int frameLevel,
            string resultDetail)
        {
            var parameters = new Dictionary<string, object>
            {
                ["rank_id"] = rankId,
                ["rank"] = rank,
                ["normal_num"] = normalNum,
                ["nick"] = nickname,
                ["avatar"] = avatar,
                ["frame_id"] = frameId,
                ["frame_level"] = frameLevel,
                ["result_detail"] = resultDetail
            };
            AddSource(parameters, source);
            Send(TrackerCatalog.Event.RankData, parameters);
        }

        public void TrackFrameGet(int frameId, string source)
        {
            var parameters = new Dictionary<string, object>
            {
                ["frame_id"] = frameId
            };
            AddSource(parameters, source);
            Send(TrackerCatalog.Event.FrameGet, parameters);
        }

        public void TrackUiLanguage(string languageCode) =>
            _sink.SetUserProperty(
                TrackerCatalog.UserProperty.UiLanguage,
                languageCode ?? string.Empty);

        public void TrackPushGuideResult(bool granted, int showCount)
        {
            Send(
                TrackerCatalog.Event.PushGuideResult,
                new Dictionary<string, object>
                {
                    ["granted"] = granted ? 1 : 0,
                    ["show_count"] = showCount,
                    ["source"] = "win_guide"
                });
        }

        public static string TransformToQuestionRotation(int transform)
        {
            int normalized = Math.Max(0, transform);
            string rotation = new[] { "0", "90", "180", "270" }
                [normalized % 4];
            if (normalized >= 8) return "V" + rotation;
            return normalized >= 4 ? "H" + rotation : rotation;
        }

        private Dictionary<string, object> ActiveStats() =>
            _activeGameType == TrackerCatalog.GameType.Daily
                ? _dailyStats
                : _mainStats;

        private void Send(
            string eventName,
            Dictionary<string, object> parameters)
        {
            if (!string.IsNullOrEmpty(_currentGameId) &&
                !parameters.ContainsKey("game_id"))
                parameters["game_id"] = _currentGameId;
            _sink.SendEvent(eventName, parameters);
        }

        private static void AddSource(
            Dictionary<string, object> parameters,
            string source)
        {
            if (!string.IsNullOrEmpty(source))
                parameters["source"] = source;
        }

        private static void Merge(
            Dictionary<string, object> target,
            IReadOnlyDictionary<string, object> values)
        {
            if (values == null) return;
            foreach (KeyValuePair<string, object> pair in values)
                target[pair.Key] = pair.Value;
        }

        private static Dictionary<string, object> Copy(
            IReadOnlyDictionary<string, object> values)
        {
            var result = new Dictionary<string, object>();
            Merge(result, values);
            return result;
        }

        private static int ReadInt(
            IReadOnlyDictionary<string, object> values,
            string key)
        {
            if (values == null ||
                !values.TryGetValue(key, out object value))
                return 0;
            try { return Convert.ToInt32(value); }
            catch (Exception) { return 0; }
        }
    }
}

``

## PATH: Assets/_Project/Scripts/Core/UI/UIRegistry.cs
``csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meowdoku.Core.UI
{
    [Serializable]
    public sealed class UIRegistryEntry
    {
        [SerializeField] private UiName name;
        [SerializeField] private UIFrameWindow prefab;

        public UiName Name => name;
        public UIFrameWindow Prefab => prefab;

        internal UIRegistryEntry(UiName name, UIFrameWindow prefab)
        {
            this.name = name;
            this.prefab = prefab;
        }
    }

    [CreateAssetMenu(
        fileName = "UIRegistry",
        menuName = "Meowdoku/UI/UI Registry")]
    public sealed class UIRegistry : ScriptableObject
    {
        [SerializeField] private List<UIRegistryEntry> entries = new();

        private readonly Dictionary<UiName, UIFrameWindow> _lookup = new();
        private bool _built;

        public bool TryGetPrefab(UiName name, out UIFrameWindow prefab)
        {
            EnsureLookup();
            return _lookup.TryGetValue(name, out prefab) && prefab != null;
        }

        public IReadOnlyList<string> ValidateEntries()
        {
            var errors = new List<string>();
            var names = new HashSet<UiName>();
            for (int index = 0; index < entries.Count; index++)
            {
                UIRegistryEntry entry = entries[index];
                if (entry == null)
                {
                    errors.Add($"Entry {index} is null.");
                    continue;
                }

                if (!names.Add(entry.Name))
                    errors.Add($"Duplicate UI name: {entry.Name}.");
                if (entry.Prefab == null)
                    errors.Add($"Missing prefab: {entry.Name}.");
            }

            return errors;
        }

        private void OnEnable()
        {
            _built = false;
        }

        private void OnValidate()
        {
            _built = false;
        }

        private void EnsureLookup()
        {
            if (_built) return;
            _lookup.Clear();
            foreach (UIRegistryEntry entry in entries)
            {
                if (entry == null || entry.Prefab == null ||
                    _lookup.ContainsKey(entry.Name))
                    continue;
                _lookup.Add(entry.Name, entry.Prefab);
            }

            _built = true;
        }

        internal void SetEntriesForTests(params UIRegistryEntry[] testEntries)
        {
            entries.Clear();
            entries.AddRange(testEntries);
            _built = false;
        }
    }
}

``

## PATH: Assets/_Project/Editor/ResultPagePrefabInstaller.cs
``csharp
using Meowdoku.Core.Localization;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Meowdoku.Editor
{
    [InitializeOnLoad]
    internal static class ResultPagePrefabInstaller
    {
        internal const string WinPrefabPath =
            "Assets/_Project/Prefabs/UI/WinPage.prefab";
        internal const string FailPrefabPath =
            "Assets/_Project/Prefabs/UI/FailPage.prefab";
        private const string FontPath = "Assets/_Project/Fonts/Roboto.ttf";
        private const string RoundedShaderPath =
            "Assets/_Project/Shaders/UIRoundedRect.shader";
        private const string WinCatPath =
            "Assets/_Project/Sprites/win/cat_victory.png";
        private const string RayPath =
            "Assets/_Project/Sprites/win/ray_light.png";
        private const string FailCatPath =
            "Assets/_Project/Sprites/fail/cat_crying.png";
        private const string FailFacePath =
            "Assets/_Project/Sprites/fail/cat_face.png";
        private const string PassPanelPath =
            "Assets/_Project/Sprites/result/pass_page_g1/panel_bg_v2.png";
        private const string CompletionIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/completion_rate.png";
        private const string MistakeIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/error_count.png";
        private const string ToolIconPath =
            "Assets/_Project/Sprites/result/pass_page_g2/hint_count.png";

        private static readonly Color Cream =
            new(1f, 0.965f, 0.925f, 1f);
        private static readonly Color Brown =
            new(0.455f, 0.31f, 0.22f, 1f);
        private static readonly Color Orange =
            new(1f, 0.541f, 0.016f, 1f);
        private static readonly Color Blue =
            new(0.447f, 0.655f, 0.859f, 1f);
        private static readonly Color PaleBlue =
            new(0.875f, 0.937f, 1f, 1f);

        static ResultPagePrefabInstaller()
        {
            EditorApplication.delayCall += InstallIfReady;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                    EditorApplication.delayCall += InstallIfReady;
            };
        }

        [MenuItem("Meowdoku/Port/Create Result Page Prefabs")]
        private static void InstallFromMenu()
        {
            InstallIfReady();
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPrefabPath);
        }

        internal static void InstallIfReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += InstallIfReady;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            Shader rounded = AssetDatabase.LoadAssetAtPath<Shader>(
                RoundedShaderPath);
            LocalizationCatalog localization =
                LocalizationCatalogAssetInstaller.GetOrCreate();
            Sprite winCat = LoadSprite(WinCatPath);
            Sprite ray = LoadSprite(RayPath);
            Sprite failCat = LoadSprite(FailCatPath);
            Sprite failFace = LoadSprite(FailFacePath);
            Sprite passPanel = LoadSprite(PassPanelPath);
            Sprite completionIcon = LoadSprite(CompletionIconPath);
            Sprite mistakeIcon = LoadSprite(MistakeIconPath);
            Sprite toolIcon = LoadSprite(ToolIconPath);
            if (font == null || rounded == null || localization == null ||
                winCat == null || ray == null || failCat == null ||
                failFace == null || passPanel == null ||
                completionIcon == null || mistakeIcon == null ||
                toolIcon == null)
                return;

            EnsureFolder("Assets/_Project/Prefabs", "UI");
            GameObject winPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(WinPrefabPath);
            if (winPrefab == null ||
                winPrefab.transform.Find("Root/PassPanel") == null ||
                winPrefab.transform.Find("Root/DailyVisuals") == null)
                Save(BuildWin(
                        font,
                        rounded,
                        localization,
                        winCat,
                        ray,
                        passPanel,
                        completionIcon,
                        mistakeIcon,
                        toolIcon),
                    WinPrefabPath);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FailPrefabPath) == null)
                Save(BuildFail(font, rounded, localization, failCat, failFace),
                    FailPrefabPath);
            UIRegistryAssetInstaller.InstallIfReady();
        }

        private static GameObject BuildWin(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            Sprite catSprite,
            Sprite raySprite,
            Sprite passPanelSprite,
            Sprite completionIcon,
            Sprite mistakeIcon,
            Sprite toolIcon)
        {
            GameObject page = CreatePage<GameWinPagePresenter>("WinPage");
            Canvas canvas = page.GetComponent<Canvas>();
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();
            RectTransform root = CreateReferenceRoot(page.transform);

            RectTransform visuals = CreateRect("Visuals", root);
            Stretch(visuals);
            Image ray = CreateImage("RayLight", visuals, raySprite, Color.white);
            SetCentered(ray.rectTransform, new Vector2(0f, 80f),
                new Vector2(1250f, 1250f));
            Image cat = CreateImage(
                "VictoryCat", visuals, catSprite, Color.white);
            SetCentered(cat.rectTransform, new Vector2(0f, 165f),
                new Vector2(500f, 500f));

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            Text title = CreateText(
                "Title", content, font, 120, "WIN_TITLE",
                Color.white, FontStyle.Bold);
            SetCentered(title.rectTransform, new Vector2(0f, 555f),
                new Vector2(900f, 180f));

            RectTransform bodyRoot = CreateRect("Body", content);
            Text body = CreateText(
                "BeatPercent", bodyRoot, font, 60, string.Empty,
                Color.white, FontStyle.Bold);
            SetCentered(body.rectTransform, new Vector2(0f, -120f),
                new Vector2(860f, 150f));

            Image stats = CreateRoundedImage(
                "Statistics", content, rounded, 30f,
                new Color(Brown.r, Brown.g, Brown.b, 0.72f));
            SetCentered(stats.rectTransform, new Vector2(0f, -120f),
                new Vector2(804f, 204f));
            CanvasGroup statsGroup =
                stats.gameObject.AddComponent<CanvasGroup>();
            Text time = CreateText(
                "Time", stats.transform, font, 48, "Time  00:00",
                Cream, FontStyle.Bold);
            SetCentered(time.rectTransform, new Vector2(-260f, 0f),
                new Vector2(250f, 150f));
            Text score = CreateText(
                "Score", stats.transform, font, 48, "Score  0",
                Cream, FontStyle.Bold);
            SetCentered(score.rectTransform, Vector2.zero,
                new Vector2(260f, 150f));
            Text combo = CreateText(
                "Combo", stats.transform, font, 48, "Combo  0",
                Cream, FontStyle.Bold);
            SetCentered(combo.rectTransform, new Vector2(260f, 0f),
                new Vector2(250f, 150f));

            RectTransform actions = CreateRect("Actions", content);
            SetCentered(actions, new Vector2(0f, -390f),
                new Vector2(820f, 180f));
            Button next = CreateRoundedButton(
                "Next", actions, font, rounded, "Level 2", Orange,
                Color.white, new Vector2(750f, 160f));

            RectTransform passPanelRoot = CreateRect("PassPanel", root);
            Stretch(passPanelRoot);
            CanvasGroup passPanelGroup =
                passPanelRoot.gameObject.AddComponent<CanvasGroup>();
            Image passPopup = CreateImage(
                "Popup", passPanelRoot, passPanelSprite, Color.white);
            passPopup.type = Image.Type.Sliced;
            passPopup.preserveAspect = false;
            SetCentered(passPopup.rectTransform, new Vector2(0f, 372f),
                new Vector2(900f, 912f));

            Text passTitle = CreateText(
                "Title", passPopup.transform, font, 80, "Perfect!",
                Brown, FontStyle.Bold);
            SetCentered(passTitle.rectTransform, new Vector2(0f, 172f),
                new Vector2(550f, 80f));

            Image passStats = CreateRoundedImage(
                "Statistics", passPopup.transform, rounded, 20f,
                new Color(0.996f, 0.945f, 0.824f, 1f));
            SetCentered(passStats.rectTransform, new Vector2(0f, -150f),
                new Vector2(800f, 512f));
            CreatePassStatRow(
                passStats.transform, font, 0, "Size", "4\u00D74",
                out Text passSizeKey, out Text passSize);
            CreatePassStatRow(
                passStats.transform, font, 1, "Time", "00:00",
                out Text passTimeKey, out Text passTime);
            CreatePassStatRow(
                passStats.transform, font, 2, "Score", "0",
                out Text passScoreKey, out Text passScore);
            CreatePassStatRow(
                passStats.transform, font, 3, "Combo", "0",
                out Text passComboKey, out Text passCombo);

            Image passExtra = CreateRoundedImage(
                "ExtraStatistics", passPopup.transform, rounded, 20f,
                new Color(0.996f, 0.945f, 0.824f, 1f));
            SetCentered(passExtra.rectTransform, new Vector2(0f, -422f),
                new Vector2(798f, 128f));
            Text passCompletion = CreatePassInfoItem(
                "CompletionRate", passExtra.transform, font,
                completionIcon, -266f, "0%");
            Text passMistake = CreatePassInfoItem(
                "MistakeCount", passExtra.transform, font,
                mistakeIcon, 0f, "0");
            Text passTools = CreatePassInfoItem(
                "ToolsUsed", passExtra.transform, font,
                toolIcon, 266f, "0");

            Text passPraise = CreateText(
                "Praise", passPanelRoot, font, 76, string.Empty,
                Brown, FontStyle.Bold);
            passPraise.supportRichText = true;
            SetCentered(passPraise.rectTransform, new Vector2(0f, -274f),
                new Vector2(880f, 200f));

            RectTransform passActions = CreateRect("Actions", passPanelRoot);
            SetCentered(passActions, new Vector2(0f, -544f),
                new Vector2(1080f, 160f));
            Button passNext = CreateRoundedButton(
                "Next", passActions, font, rounded, "Level 2", Orange,
                Color.white, new Vector2(784f, 160f));
            passPanelRoot.gameObject.SetActive(false);

            RectTransform dailyVisuals = CreateRect("DailyVisuals", root);
            Stretch(dailyVisuals);
            CanvasGroup dailyGroup =
                dailyVisuals.gameObject.AddComponent<CanvasGroup>();
            Image dailyRay = CreateImage(
                "RayLight", dailyVisuals, raySprite, Color.white);
            SetCentered(
                dailyRay.rectTransform,
                new Vector2(0f, 80f),
                new Vector2(1250f, 1250f));
            Image dailyCat = CreateImage(
                "VictoryCatStaticAdapter",
                dailyVisuals,
                catSprite,
                Color.white);
            SetCentered(
                dailyCat.rectTransform,
                new Vector2(0f, 120f),
                new Vector2(560f, 560f));

            Text dailyTitle = CreateText(
                "Title", dailyVisuals, font, 120, "DAILY_WIN_TITLE",
                Color.white, FontStyle.Bold);
            SetCentered(
                dailyTitle.rectTransform,
                new Vector2(0f, 690f),
                new Vector2(900f, 180f));
            Outline dailyTitleOutline =
                dailyTitle.gameObject.AddComponent<Outline>();
            dailyTitleOutline.effectColor =
                new Color(0.8f, 0.349f, 0f, 1f);
            dailyTitleOutline.effectDistance = new Vector2(6f, -6f);

            Text dailyTime = CreateText(
                "Time", dailyVisuals, font, 70, string.Empty,
                Cream, FontStyle.Normal);
            dailyTime.supportRichText = true;
            SetCentered(
                dailyTime.rectTransform,
                new Vector2(0f, -360f),
                new Vector2(880f, 115f));
            Text dailyBeat = CreateText(
                "Beat", dailyVisuals, font, 70, string.Empty,
                new Color(1f, 0.89f, 0.459f, 1f), FontStyle.Normal);
            dailyBeat.supportRichText = true;
            SetCentered(
                dailyBeat.rectTransform,
                new Vector2(0f, -520f),
                new Vector2(900f, 200f));

            RectTransform dailyActions = CreateRect(
                "Actions",
                dailyVisuals);
            SetCentered(
                dailyActions,
                new Vector2(0f, -730f),
                new Vector2(820f, 180f));
            Button dailyContinue = CreateRoundedButton(
                "Continue", dailyActions, font, rounded, "WIN_CONTINUE",
                Orange, Color.white, new Vector2(784f, 160f));
            dailyVisuals.gameObject.SetActive(false);

            SerializedObject data =
                new(page.GetComponent<GameWinPagePresenter>());
            ConfigureFrame(data, canvas, pageGroup, true, 0.85f);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", contentGroup);
            SetReference(data, "defaultVisuals", visuals.gameObject);
            SetReference(data, "rayLight", ray.rectTransform);
            SetReference(data, "victoryCat", cat.rectTransform);
            SetReference(data, "titleText", title);
            SetReference(data, "bodyRoot", bodyRoot.gameObject);
            SetReference(data, "bodyText", body);
            SetReference(data, "statisticsRoot", stats.gameObject);
            SetReference(data, "statisticsGroup", statsGroup);
            SetReference(data, "timeText", time);
            SetReference(data, "scoreText", score);
            SetReference(data, "comboText", combo);
            SetReference(data, "nextButtonText",
                next.GetComponentInChildren<Text>(true));
            SetReference(data, "nextButton", next);
            SetReference(data, "passPanelRoot", passPanelRoot.gameObject);
            SetReference(data, "passPanelPopup", passPopup.rectTransform);
            SetReference(data, "passPanelGroup", passPanelGroup);
            SetReference(data, "passTitleText", passTitle);
            SetReference(data, "passPraiseText", passPraise);
            SetReference(data, "passPraiseRect", passPraise.rectTransform);
            SetReference(data, "passStatsRoot", passStats.rectTransform);
            SetReference(data, "passActionsRect", passActions);
            SetReference(data, "passSizeKeyText", passSizeKey);
            SetReference(data, "passTimeKeyText", passTimeKey);
            SetReference(data, "passScoreKeyText", passScoreKey);
            SetReference(data, "passComboKeyText", passComboKey);
            SetReference(data, "passSizeText", passSize);
            SetReference(data, "passTimeText", passTime);
            SetReference(data, "passScoreText", passScore);
            SetReference(data, "passComboText", passCombo);
            SetReference(data, "passExtraRoot", passExtra.gameObject);
            SetReference(data, "passCompletionText", passCompletion);
            SetReference(data, "passMistakeText", passMistake);
            SetReference(data, "passToolsText", passTools);
            SetReference(data, "passNextButtonText",
                passNext.GetComponentInChildren<Text>(true));
            SetReference(data, "passNextButton", passNext);
            SetReference(data, "dailyVisuals", dailyVisuals.gameObject);
            SetReference(data, "dailyContent", dailyVisuals);
            SetReference(data, "dailyContentGroup", dailyGroup);
            SetReference(data, "dailyRayLight", dailyRay.rectTransform);
            SetReference(data, "dailyVictoryCat", dailyCat.rectTransform);
            SetReference(data, "dailyTitleText", dailyTitle);
            SetReference(data, "dailyTimeText", dailyTime);
            SetReference(data, "dailyBeatText", dailyBeat);
            SetReference(
                data,
                "dailyContinueText",
                dailyContinue.GetComponentInChildren<Text>(true));
            SetReference(data, "dailyContinueButton", dailyContinue);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject BuildFail(
            Font font,
            Shader rounded,
            LocalizationCatalog localization,
            Sprite catSprite,
            Sprite faceSprite)
        {
            GameObject page = CreatePage<GameFailPagePresenter>("FailPage");
            Canvas canvas = page.GetComponent<Canvas>();
            CanvasGroup pageGroup = page.GetComponent<CanvasGroup>();

            Image overlay = CreateImage(
                "Overlay", page.transform, null, Color.black);
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;
            CanvasGroup overlayGroup =
                overlay.gameObject.AddComponent<CanvasGroup>();

            RectTransform root = CreateReferenceRoot(page.transform);
            RectTransform visuals = CreateRect("Visuals", root);
            Stretch(visuals);
            Image cat = CreateImage(
                "CryingCat", visuals, catSprite, Color.white);
            SetCentered(cat.rectTransform, new Vector2(0f, 260f),
                new Vector2(520f, 520f));

            RectTransform content = CreateRect("Content", root);
            Stretch(content);
            CanvasGroup contentGroup =
                content.gameObject.AddComponent<CanvasGroup>();
            Text title = CreateText(
                "Title", content, font, 120, "FAIL_TITLE_FISH",
                Color.white, FontStyle.Bold);
            SetCentered(title.rectTransform, new Vector2(0f, 675f),
                new Vector2(900f, 180f));

            RectTransform remainingRoot = CreateRect("Remaining", content);
            SetCentered(remainingRoot, new Vector2(0f, -15f),
                new Vector2(760f, 100f));
            Image face = CreateImage(
                "CatFace", remainingRoot, faceSprite, Color.white);
            SetCentered(face.rectTransform, new Vector2(-190f, 0f),
                new Vector2(86f, 86f));
            Text remaining = CreateText(
                "Count", remainingRoot, font, 56, "Remaining: 0",
                Color.white, FontStyle.Bold);
            SetCentered(remaining.rectTransform, new Vector2(90f, 0f),
                new Vector2(520f, 90f));

            RectTransform encourageRoot = CreateRect("Encourage", content);
            SetCentered(encourageRoot, new Vector2(0f, -155f),
                new Vector2(900f, 120f));
            Text encourage = CreateText(
                "Label", encourageRoot, font, 54, string.Empty,
                Color.white, FontStyle.Bold);
            Stretch(encourage.rectTransform);

            RectTransform actions = CreateRect("Actions", content);
            Stretch(actions);
            RectTransform reviveRoot = CreateRect("Revive", actions);
            SetCentered(reviveRoot, new Vector2(0f, -345f),
                new Vector2(820f, 190f));
            Button revive = CreateRoundedButton(
                "ReviveButton", reviveRoot, font, rounded, "Revive", Blue,
                Color.white, new Vector2(780f, 160f));
            Text reviveText = revive.GetComponentInChildren<Text>(true);
            Text reviveSubtitle = CreateText(
                "Subtitle", revive.transform, font, 36, string.Empty,
                Color.white, FontStyle.Normal);
            SetCentered(reviveSubtitle.rectTransform, new Vector2(0f, -49f),
                new Vector2(700f, 45f));

            RectTransform restartRoot = CreateRect("Restart", actions);
            SetCentered(restartRoot, new Vector2(0f, -555f),
                new Vector2(820f, 190f));
            Button restart = CreateRoundedButton(
                "RestartButton", restartRoot, font, rounded, "Restart",
                PaleBlue, Brown, new Vector2(780f, 160f));

            SerializedObject data =
                new(page.GetComponent<GameFailPagePresenter>());
            ConfigureFrame(data, canvas, pageGroup, false, 0f);
            SetReference(data, "overlayGroup", overlayGroup);
            SetReference(data, "content", content);
            SetReference(data, "contentGroup", contentGroup);
            SetReference(data, "titleText", title);
            SetReference(data, "remainingText", remaining);
            SetReference(data, "encourageRoot", encourageRoot.gameObject);
            SetReference(data, "encourageText", encourage);
            SetReference(data, "reviveRoot", reviveRoot.gameObject);
            SetReference(data, "reviveText", reviveText);
            SetReference(data, "reviveSubtitleText", reviveSubtitle);
            SetReference(data, "reviveButton", revive);
            SetReference(data, "restartText",
                restart.GetComponentInChildren<Text>(true));
            SetReference(data, "restartButton", restart);
            SetReference(data, "localization", localization);
            data.ApplyModifiedPropertiesWithoutUndo();
            return page;
        }

        private static GameObject CreatePage<T>(string name)
            where T : Component
        {
            var page = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(GraphicRaycaster),
                typeof(T));
            Stretch((RectTransform)page.transform);
            page.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            return page;
        }

        private static RectTransform CreateReferenceRoot(Transform parent)
        {
            RectTransform root = CreateRect("Root", parent);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(1080f, 0f);
            return root;
        }

        private static void CreatePassStatRow(
            Transform parent,
            Font font,
            int index,
            string keyValue,
            string displayValue,
            out Text keyText,
            out Text valueText)
        {
            RectTransform row = CreateRect("Row" + (index + 1), parent);
            SetCentered(row, new Vector2(0f, 192f - index * 128f),
                new Vector2(800f, 128f));
            keyText = CreateText(
                "Key", row, font, 56, keyValue, Brown, FontStyle.Bold);
            keyText.alignment = TextAnchor.MiddleLeft;
            SetCentered(keyText.rectTransform, new Vector2(-190f, 0f),
                new Vector2(300f, 128f));
            valueText = CreateText(
                "Value", row, font, 56, displayValue, Brown,
                FontStyle.Bold);
            valueText.alignment = TextAnchor.MiddleRight;
            SetCentered(valueText.rectTransform, new Vector2(210f, 0f),
                new Vector2(300f, 128f));

            if (index >= 3) return;
            Image divider = CreateImage(
                "Divider", row, null,
                new Color(Brown.r, Brown.g, Brown.b, 0.05f));
            SetCentered(divider.rectTransform, new Vector2(0f, -62f),
                new Vector2(720f, 4f));
        }

        private static Text CreatePassInfoItem(
            string name,
            Transform parent,
            Font font,
            Sprite iconSprite,
            float x,
            string value)
        {
            RectTransform item = CreateRect(name, parent);
            SetCentered(item, new Vector2(x, 0f), new Vector2(266f, 128f));
            Image icon = CreateImage("Icon", item, iconSprite, Color.white);
            SetCentered(icon.rectTransform, new Vector2(-45f, 0f),
                new Vector2(80f, 80f));
            Text text = CreateText(
                "Value", item, font, 52, value, Brown, FontStyle.Bold);
            text.alignment = TextAnchor.MiddleLeft;
            SetCentered(text.rectTransform, new Vector2(65f, 0f),
                new Vector2(130f, 100f));
            return text;
        }

        private static Button CreateRoundedButton(
            string name,
            Transform parent,
            Font font,
            Shader shader,
            string label,
            Color background,
            Color foreground,
            Vector2 size)
        {
            Image image = CreateRoundedImage(
                name, parent, shader, size.y * 0.5f, background);
            SetCentered(image.rectTransform, Vector2.zero, size);
            image.raycastTarget = true;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Text text = CreateText(
                "Label", image.transform, font, 70, label,
                foreground, FontStyle.Bold);
            Stretch(text.rectTransform);
            return button;
        }

        private static Image CreateRoundedImage(
            string name,
            Transform parent,
            Shader shader,
            float radius,
            Color color)
        {
            Image image = CreateImage(name, parent, null, color);
            image.gameObject.AddComponent<RoundedImageView>()
                .Configure(image, shader, radius);
            return image;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            string value,
            Color color,
            FontStyle style)
        {
            RectTransform rect = CreateRect(name, parent);
            Text text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = size;
            text.text = value;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static void ConfigureFrame(
            SerializedObject data,
            Canvas canvas,
            CanvasGroup canvasGroup,
            bool showMask,
            float opacity)
        {
            data.FindProperty("uiLayer").intValue = (int)UiLayer.Default;
            data.FindProperty("isFullscreen").boolValue = false;
            data.FindProperty("showMask").boolValue = showMask;
            data.FindProperty("maskOpacity").floatValue = opacity;
            data.FindProperty("rootCanvas").objectReferenceValue = canvas;
            data.FindProperty("rootCanvasGroup").objectReferenceValue = canvasGroup;
        }

        private static void SetReference(
            SerializedObject data,
            string propertyName,
            Object value)
        {
            SerializedProperty property = data.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetCentered(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite) return sprite;
            }
            return null;
        }

        private static void Save(GameObject page, string path)
        {
            try
            {
                PrefabUtility.SaveAsPrefabAsset(page, path);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                Object.DestroyImmediate(page);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}

``

## PATH: Assets/_Project/Tests/EditMode/SettingsPageContractTests.cs
``csharp
using Meowdoku.Core.Config;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class SettingsPageContractTests
    {
        [Test]
        public void OfflineOutgameLayout_MatchesSourceDefaults()
        {
            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                "en_US");

            Assert.That(state.ShowMusic, Is.False);
            Assert.That(state.ShowSound, Is.True);
            Assert.That(state.ShowVibration, Is.True);
            Assert.That(state.ShowPeople, Is.True);
            Assert.That(state.VisibleToggleCount, Is.EqualTo(3));
            Assert.That(state.ToggleHorizontalSeparation, Is.EqualTo(30));
            Assert.That(state.ToggleScale, Is.EqualTo(1f));
            Assert.That(state.ShowLanguageButton, Is.False);
            Assert.That(state.ShowLanguageDropdown, Is.False);
            Assert.That(state.ShowPattern, Is.False);
            Assert.That(state.ShowFeedback, Is.True);
            Assert.That(state.ShowRestart, Is.False);
            Assert.That(state.ShowTerms, Is.True);
            Assert.That(state.ShowVersion, Is.True);
            Assert.That(state.BottomSpacerMinimum, Is.EqualTo(30f));
        }

        [Test]
        public void OfflineGameLayout_HidesOutgameRowsAndUsesGameSpacer()
        {
            SettingsPresentationState state = SettingsPageContract.Resolve(
                true,
                "en_US");

            Assert.That(state.ShowPattern, Is.False);
            Assert.That(state.ShowHowToPlay, Is.False);
            Assert.That(state.ShowRestart, Is.True);
            Assert.That(state.ShowTerms, Is.False);
            Assert.That(state.ShowVersion, Is.False);
            Assert.That(state.ShowCmp, Is.False);
            Assert.That(state.BottomSpacerMinimum, Is.EqualTo(90f));
        }

        [Test]
        public void PopupLanguage_RemainsVisibleForEnglishSystemLocale()
        {
            var language = new SettingsLanguageConfig();
            language.SetDebugOverride(SettingsLanguageConfig.ValuePopup);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                "en_US",
                language: language);

            Assert.That(state.ShowLanguageButton, Is.True);
            Assert.That(state.ShowLanguageDropdown, Is.False);
        }

        [TestCase("en_US", false)]
        [TestCase("en-US", false)]
        [TestCase("ja_JP", true)]
        public void DropdownLanguage_UsesSourceEnglishSuppression(
            string locale,
            bool expectedVisible)
        {
            var language = new SettingsLanguageConfig();
            language.SetDebugOverride(SettingsLanguageConfig.ValueDropdown);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                false,
                locale,
                language: language);

            Assert.That(state.ShowLanguageDropdown, Is.EqualTo(expectedVisible));
            Assert.That(state.ShowToggleContainer, Is.EqualTo(expectedVisible));
        }

        [Test]
        public void GameVariants_ShowPatternHowToPlayAndUnreadDot()
        {
            var blindMode = new BlindModConfig();
            var ruleText = new RuleTextConfig();
            blindMode.SetDebugOverride(BlindModConfig.ValueHideOnFilled);
            ruleText.SetDebugOverride(RuleTextConfig.ValueSettingEntry);

            SettingsPresentationState state = SettingsPageContract.Resolve(
                true,
                "vi_VN",
                tutorialDone: true,
                patternSwitchDotDismissed: false,
                blindMode: blindMode,
                ruleText: ruleText);

            Assert.That(state.ShowPattern, Is.True);
            Assert.That(state.ShowPatternDot, Is.True);
            Assert.That(state.ShowHowToPlay, Is.True);
            Assert.That(state.ShowRestart, Is.True);
            Assert.That(state.ShowToggleContainer, Is.True);
        }

        [Test]
        public void GenericPopupTiming_MatchesSourceAnimationResource()
        {
            Assert.That(SettingsPageContract.PopupMarkerSeconds,
                Is.EqualTo(0.3f));
            Assert.That(SettingsPageContract.PopupLengthSeconds,
                Is.EqualTo(0.6192876f));
            Assert.That(SettingsPageContract.PopupOpenOvershootSeconds,
                Is.EqualTo(0.09963459f));
            Assert.That(SettingsPageContract.PopupOpenFadeSeconds,
                Is.EqualTo(0.05483741f));
            Assert.That(SettingsPageContract.PopupCloseOvershootSeconds,
                Is.EqualTo(0.1492851f));
            Assert.That(SettingsPageContract.PopupCloseFadeStartSeconds,
                Is.EqualTo(0.2666667f));
        }

        [Test]
        public void ToastTimingAndPlacement_MatchSourceScript()
        {
            Assert.That(SourceToastView.MaximumWidth, Is.EqualTo(870f));
            Assert.That(SourceToastView.SourceTopY, Is.EqualTo(750f));
            Assert.That(SourceToastView.FloatDistance, Is.EqualTo(50f));
            Assert.That(SourceToastView.FadeInSeconds, Is.EqualTo(0.15f));
            Assert.That(SourceToastView.HoldSeconds, Is.EqualTo(1.2f));
            Assert.That(SourceToastView.FadeOutSeconds, Is.EqualTo(0.2f));
            Assert.That(SourceToastView.MoveSeconds, Is.EqualTo(1.55f));
        }
    }
}

``

## PATH: Assets/_Project/Tests/EditMode/GameStateServiceTests.cs
``csharp
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Config;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class GameStateServiceTests
    {
        [Test]
        public void BankProgress_UsesSourceKeyShapeAndTierRule()
        {
            var service = new GameStateService(new GameStateData());

            service.AdvanceBankIndex(7, 3, "N", false);
            service.AdvanceBankIndex(7, 3, "H", false);
            service.AdvanceBankIndex(7, 3, "H", false);

            Assert.That(service.GetBankIndex(7, 3, ""), Is.EqualTo(1));
            Assert.That(service.GetBankIndex(7, 3, "N"), Is.EqualTo(1));
            Assert.That(service.GetBankIndex(7, 3, "H"), Is.EqualTo(2));
            Assert.That(service.Data.BankProgress, Contains.Key("7_3"));
            Assert.That(service.Data.BankProgress, Contains.Key("7_3_H"));
        }

        [Test]
        public void PersistFalse_BatchesUntilCommit()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.AdvanceBankIndex(6, 2, "", false);
            service.AdvanceBankIndex(6, 2, "", false);
            Assert.That(store.SaveCount, Is.Zero);

            Assert.That(service.CommitBankProgress(), Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(store.LastData.BankProgress["6_2"], Is.EqualTo(2));
        }

        [Test]
        public void PersistTrue_SavesImmediately()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.AdvanceBankIndex(5, 1);

            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MainProgress_DefaultPreservesLegacyMigrationTrigger()
        {
            var service = new GameStateService(new GameStateData());

            Dictionary<string, object> progress = service.GetMainProgress(8, 4, "H");

            Assert.That(progress, Contains.Key("lk_mod"));
            Assert.That(progress, Contains.Key("regular"));
            Assert.That(progress, Contains.Key("lkstyle"));
            Assert.That(progress, Contains.Key("transform"));
            Assert.That(progress, Does.Not.ContainKey("idx"));
            Assert.That(service.Data.MainBankProgress, Contains.Key("8_4_H"));
        }

        [Test]
        public void LkModifiedProgress_IgnoresTierAndDefaultsIndexToZero()
        {
            var service = new GameStateService(new GameStateData());

            Dictionary<string, object> progress = service.GetLkModifiedProgress(10, 2);

            Assert.That(progress["idx"], Is.EqualTo(0));
            Assert.That(service.Data.LkModifiedProgress, Contains.Key("10_2"));
        }

        [Test]
        public void Snapshot_IsDeepCopyOfNestedProgress()
        {
            var service = new GameStateService(new GameStateData());
            Dictionary<string, object> progress = service.GetMainProgress(9, 3);
            progress["transform"] = 2;

            Dictionary<string, object> snapshot = service.GetMainBankProgressSnapshot();
            var snapshotProgress = (Dictionary<string, object>)snapshot["9_3"];
            snapshotProgress["transform"] = 7;

            Assert.That(progress["transform"], Is.EqualTo(2));
        }

        [Test]
        public void ProgressionSetters_PreserveValuesAndPersistEachCall()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetCurrentLevel(51);
            service.SetTutorialDone(true);
            service.SetCurrentStrategy(4);
            service.SetAppliedLocale("vi");

            Assert.That(service.CurrentLevel, Is.EqualTo(51));
            Assert.That(service.TutorialDone, Is.True);
            Assert.That(service.CurrentStrategy, Is.EqualTo(4));
            Assert.That(service.AppliedLocale, Is.EqualTo("vi"));
            Assert.That(store.SaveCount, Is.EqualTo(4));
        }

        [Test]
        public void PatternSettings_PersistAndDismissDotsIdempotently()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetPatternModeOn(true);
            service.MarkPatternEntryDotDismissed();
            service.MarkPatternEntryDotDismissed();
            service.MarkPatternSwitchDotDismissed();
            service.MarkPatternSwitchDotDismissed();

            Assert.That(service.PatternModeOn, Is.True);
            Assert.That(service.PatternEntryDotDismissed, Is.True);
            Assert.That(service.PatternSwitchDotDismissed, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void FirstSession_PersistsFalseButRemainsTrueForCurrentRuntime()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            Assert.That(service.IsFirstSession, Is.True);
            service.ConsumeFirstSessionPersist();

            Assert.That(service.Data.IsFirstSession, Is.False);
            Assert.That(service.IsFirstSession, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            service.MarkFirstSessionDone();
            Assert.That(service.IsFirstSession, Is.False);
        }

        [Test]
        public void SplashDate_IsFirstOncePerDayAndPersistsSourceKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(
                new GameStateData(),
                store,
                dateProvider: new DateProvider("2026-08-10"));

            Assert.That(service.MarkSplashShownToday(), Is.True);
            Assert.That(service.MarkSplashShownToday(), Is.False);
            Assert.That(service.LastSplashDate, Is.EqualTo("2026-08-10"));
            Assert.That(store.SaveCount, Is.EqualTo(1));

            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(
                progress["last_splash_date"],
                Is.EqualTo("2026-08-10"));
            Assert.That(
                GameStateData.FromDocuments(player, null).LastSplashDate,
                Is.EqualTo("2026-08-10"));
        }

        [Test]
        public void FreeReviveFlag_IsIdempotentAndPersistsSourceKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkReviveFreeUsed();
            service.MarkReviveFreeUsed();

            Assert.That(service.HasUsedReviveFree, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(progress["has_used_revive_free"], Is.True);
            Assert.That(
                GameStateData.FromDocuments(player, null).HasUsedReviveFree,
                Is.True);
        }

        [Test]
        public void LastWinBeatPercent_PersistsSourceProgressKey()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetLastWinBeatPercent(83.7f);
            service.SetLastWinBeatPercent(83.7f);

            Assert.That(service.LastWinBeatPercent, Is.EqualTo(83.7f));
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Dictionary<string, object> player =
                service.Data.ToPlayerDocument();
            var progress = (Dictionary<string, object>)player["progress"];
            Assert.That(progress["last_win_beat_percent"], Is.EqualTo(83.7f));
            Assert.That(
                GameStateData.FromDocuments(player, null).LastWinBeatPercent,
                Is.EqualTo(83.7f));
        }

        [Test]
        public void MusicUserChoice_BlocksLaterDefaultInitialization()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.SetMusicOn(false);
            service.InitMusicDefault(true);

            Assert.That(service.MusicOn, Is.False);
            Assert.That(service.Data.MusicUserModified, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MusicDefault_OnlyPersistsWhenItChangesUntouchedValue()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.InitMusicDefault(true);
            Assert.That(store.SaveCount, Is.Zero);

            service.InitMusicDefault(false);
            Assert.That(service.MusicOn, Is.False);
            Assert.That(service.Data.MusicUserModified, Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void SettingsSetters_SaveAndVibrationUpdatesSink()
        {
            var store = new CountingStore();
            var vibration = new RecordingVibrationSink();
            var service = new GameStateService(new GameStateData(), store, vibration);

            Assert.That(vibration.LastEnabled, Is.True);
            Assert.That(vibration.CallCount, Is.EqualTo(1));

            service.SetSoundOn(false);
            service.SetVibrationOn(false);
            service.SetPeopleOn(false);

            Assert.That(service.SoundOn, Is.False);
            Assert.That(service.VibrationOn, Is.False);
            Assert.That(service.PeopleOn, Is.False);
            Assert.That(vibration.LastEnabled, Is.False);
            Assert.That(vibration.CallCount, Is.EqualTo(2));
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void ToolDecrease_MarksUsagePersistsAndEmitsSourceSignal()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            string emittedKind = null;
            int emittedCount = -1;
            service.ToolCountChanged += (kind, count) =>
            {
                emittedKind = kind;
                emittedCount = count;
            };

            service.SetToolCount("hint", 4);

            Assert.That(service.GetToolCount("hint"), Is.EqualTo(4));
            Assert.That(service.HasUsedTool, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
            Assert.That(emittedKind, Is.EqualTo("hint"));
            Assert.That(emittedCount, Is.EqualTo(4));
        }

        [Test]
        public void PropHighlightShown_PersistsOnceAndRuntimeFlagsResetTogether()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkPropHighlightShown();
            service.MarkPropHighlightShown();
            service.MarkCurrentLevelDirty();
            service.MarkDdaToolOrReviveUsed();
            service.MarkDdaReviveUsed();

            Assert.That(service.HasPropHighlightShown, Is.True);
            Assert.That(service.IsCurrentLevelDirty, Is.True);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.True);
            Assert.That(service.WasDdaReviveUsed, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));

            service.ResetCurrentLevelRuntimeFlags();

            Assert.That(service.IsCurrentLevelDirty, Is.False);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.False);
            Assert.That(service.WasDdaReviveUsed, Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void UndoTool_RemainsSerializedLegacyFieldButRuntimeApiIgnoresIt()
        {
            var store = new CountingStore();
            var data = new GameStateData { ToolUndo = 3 };
            var service = new GameStateService(data, store);
            bool emitted = false;
            service.ToolCountChanged += (kind, count) => emitted = true;

            service.SetToolCount("undo", 1);

            Assert.That(service.GetToolCount("undo"), Is.Zero);
            Assert.That(data.ToolUndo, Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.Zero);
            Assert.That(emitted, Is.False);
        }

        [Test]
        public void RetryPuzzle_ReturnsParametersOnlyForMatchingLevel()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            var parameters = new Dictionary<string, object> { { "seed", 42 } };

            service.SetRetryPuzzle(12, parameters);

            Assert.That(service.GetRetryPuzzle(11), Is.Empty);
            Assert.That(service.GetRetryPuzzle(12), Is.SameAs(parameters));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkPreCatRevived_IsIdempotent()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);

            service.MarkPreCatRevived();
            service.MarkPreCatRevived();

            Assert.That(service.Data.PreCatRevivedThisLevel, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void ConsumePreCatPending_ReturnsThenClearsFlagsOnce()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                PreCatPendingHard = true,
                PreCatPendingStruggle = false,
                PreCatPendingDemote = true
            };
            var service = new GameStateService(data, store);

            Dictionary<string, object> first = service.ConsumePreCatPending();
            Dictionary<string, object> second = service.ConsumePreCatPending();

            Assert.That(first["hard"], Is.True);
            Assert.That(first["struggle"], Is.False);
            Assert.That(first["demote"], Is.True);
            Assert.That(second["hard"], Is.False);
            Assert.That(second["struggle"], Is.False);
            Assert.That(second["demote"], Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void PreCatLock_IsRestoredOnlyForMatchingPositiveLevel()
        {
            var store = new CountingStore();
            var service = new GameStateService(new GameStateData(), store);
            var position = new UnityEngine.Vector2Int(2, 4);

            service.SetPreCatLock(15, "2", position);

            Assert.That(service.GetPreCatLock(14)["locked"], Is.False);
            Dictionary<string, object> matching = service.GetPreCatLock(15);
            Assert.That(matching["locked"], Is.True);
            Assert.That(matching["pre_type"], Is.EqualTo("2"));
            Assert.That(matching["position"], Is.EqualTo(position));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void EndgameSnapshot_AddsVersionAndUsesImmediateStore()
        {
            var store = new CombinedStore();
            var service = new GameStateService(
                new GameStateData(), store, null, store, "1.2.3");
            var snapshot = new Dictionary<string, object> { { "lives", 2 } };

            Assert.That(service.SetEndgameSnapshot(snapshot), Is.True);

            Assert.That(snapshot["app_version"], Is.EqualTo("1.2.3"));
            Assert.That(service.GetEndgameSnapshot(), Is.SameAs(snapshot));
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
            Assert.That(store.RequestedEndgameSaveCount, Is.Zero);
        }

        [Test]
        public void EndgameStats_RouteDailyAndMainAndPreserveSaveModes()
        {
            var store = new CombinedStore();
            var service = new GameStateService(new GameStateData(), store, null, store);

            service.IncrementGameTotalStat("main", "step", 2);
            service.IncrementGameTotalStat("daily", "step", 3);
            service.SetPersistedGameId("daily", "daily-id");
            service.PersistGameRoundStats(
                "main",
                new Dictionary<string, object> { { "score", 100 } });

            Assert.That(service.GetGameTotalStat("main", "step"), Is.EqualTo(2));
            Assert.That(service.GetGameTotalStat("daily", "step"), Is.EqualTo(3));
            Assert.That(service.GetPersistedGameId("daily"), Is.EqualTo("daily-id"));
            Assert.That(service.GetGameRoundStats("main")["score"], Is.EqualTo(100));
            Assert.That(store.RequestedEndgameSaveCount, Is.EqualTo(3));
            Assert.That(store.ImmediateEndgameSaveCount, Is.EqualTo(1));
        }

        [Test]
        public void RoundStats_AreCopiedOnSetAndGet()
        {
            var store = new CombinedStore();
            var service = new GameStateService(new GameStateData(), store, null, store);
            var source = new Dictionary<string, object> { { "score", 20 } };

            service.PersistGameRoundStats("main", source);
            source["score"] = 99;
            Dictionary<string, object> restored = service.GetGameRoundStats("main");
            restored["score"] = 50;

            Assert.That(service.Data.MainGameRoundStats["score"], Is.EqualTo(20));
        }

        [Test]
        public void RecordPuzzle_ReturnsPreviousMatchAndKeepsIndependentSnapshots()
        {
            var store = new CountingStore();
            var data = new GameStateData();
            data.BankProgress["4_1"] = 3;
            var service = new GameStateService(data, store);

            Assert.That(service.RecordPuzzle("pid", 10, "1.0", "regular"), Is.Empty);
            data.BankProgress["4_1"] = 4;
            Dictionary<string, object> previous =
                service.RecordPuzzle("pid", 11, "1.1", "regular");

            Assert.That(previous["level"], Is.EqualTo(10));
            var previousBank = (Dictionary<string, object>)previous["bank_progress"];
            Assert.That(previousBank["4_1"], Is.EqualTo(3));
            previousBank["4_1"] = 99;
            var storedFirst = (Dictionary<string, object>)service.GetRecentPuzzles()[0];
            var storedBank = (Dictionary<string, object>)storedFirst["bank_progress"];
            Assert.That(storedBank["4_1"], Is.EqualTo(3));
            Assert.That(store.SaveCount, Is.EqualTo(2));
        }

        [Test]
        public void RecordPuzzle_TrimsHistoryToSourceLimit()
        {
            var service = new GameStateService(new GameStateData());
            for (int index = 0; index < 101; index++)
                service.RecordPuzzle("p" + index, index);

            List<object> recent = service.GetRecentPuzzles();
            Assert.That(recent.Count, Is.EqualTo(100));
            Assert.That(((Dictionary<string, object>)recent[0])["puzzle_id"], Is.EqualTo("p1"));
        }

        [Test]
        public void DailyFirstEasy_EvaluatesConsumesAndPersistsDate()
        {
            var store = new CountingStore();
            var data = new GameStateData { CurrentLevel = 12 };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-08"));

            service.EvaluateDailyFirstEasy();
            Assert.That(service.IsDailyFirstEasyAvailable, Is.True);
            service.ConsumeDailyFirstEasy(true);

            Assert.That(data.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
            Assert.That(service.IsDailyFirstEasyAvailable, Is.False);
            Assert.That(service.IsCurrentLevelDailyFirstEasy, Is.True);
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void DailyFirstEasy_ExistingPlayedSnapshotConsumesOpportunityOnEvaluation()
        {
            var store = new CountingStore();
            var data = new GameStateData { CurrentLevel = 12 };
            data.EndgameSnapshot = new Dictionary<string, object>
            {
                { "level", 12 }, { "lives", 2 },
                { "prefill_positions", new List<object>() },
                { "placed_cats", new List<object> { new object() } },
                { "marks", new List<object>() }, { "errors", new List<object>() }
            };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-08"));

            service.EvaluateDailyFirstEasy();

            Assert.That(service.IsDailyFirstEasyAvailable, Is.False);
            Assert.That(data.DailyFirstEasyDate, Is.EqualTo("2026-08-08"));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void GameFinished_RollsDailyCountersThenCountsExactlyOnce()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                TodayDate = "2026-08-05",
                TodaySessionCount = 4,
                TodayPlayedCount = 9,
                TodayActiveSeconds = 30,
                ActiveDays = 2,
                RecentWinCountsByDay = new Dictionary<string, object>
                {
                    { "2026-08-05", 1 },
                    { "2026-08-06", 2 },
                    { "2026-08-07", 3 }
                }
            };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-09"));

            service.OnGameFinished();

            Assert.That(service.SessionPlayedCount, Is.EqualTo(1));
            Assert.That(data.TodayDate, Is.EqualTo("2026-08-09"));
            Assert.That(data.LastDaySessionCount, Is.EqualTo(4));
            Assert.That(data.TodaySessionCount, Is.Zero);
            Assert.That(data.TodayPlayedCount, Is.EqualTo(1));
            Assert.That(data.TodayActiveSeconds, Is.Zero);
            Assert.That(data.ActiveDays, Is.EqualTo(3));
            Assert.That(data.RecentWinCountsByDay, Does.Not.ContainKey("2026-08-05"));
            Assert.That(data.RecentWinCountsByDay, Does.Not.ContainKey("2026-08-06"));
            Assert.That(data.RecentWinCountsByDay, Contains.Key("2026-08-07"));
            Assert.That(store.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void TwoFailsThenCleanRetryWin_PreservesSourceDirtyAndRetrySemantics()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                CurrentLevel = 21,
                CurrentStrategy = 2,
                RetryPuzzleLevel = 21,
                RetryPuzzleParameters = new Dictionary<string, object> { { "seed", 3 } }
            };
            var service = new GameStateService(data, store);
            var settled = new List<bool>();
            service.LevelSettled += settled.Add;

            service.OnLevelFailed(21);
            service.OnLevelFailed(21);
            service.ClearCurrentLevelDirty();
            service.OnLevelWon(21);

            Assert.That(data.CurrentLevel, Is.EqualTo(22));
            Assert.That(data.CurrentStrategy, Is.EqualTo(1));
            Assert.That(data.LastLevelCleanWin, Is.True);
            Assert.That(data.PreCatPendingStruggle, Is.True);
            Assert.That(data.PreCatPendingDemote, Is.True);
            Assert.That(data.PreCatFailCount, Is.Zero);
            Assert.That(data.PreCatFailLevel, Is.Zero);
            Assert.That(data.RetryPuzzleLevel, Is.Zero);
            Assert.That(data.RetryPuzzleParameters, Is.Empty);
            Assert.That(service.IsCurrentLevelRetried, Is.False);
            Assert.That(settled, Is.EqualTo(new[] { false, false, true }));
            Assert.That(store.SaveCount, Is.EqualTo(3));
        }

        [Test]
        public void ToolDda_WinDemotesImmediatelyWhenNextLevelIsNotSkipped()
        {
            var dda = new DdaRankConfig();
            dda.SetDebugOverride(DdaRankConfig.ValueToolRevive);
            var data = new GameStateData { CurrentLevel = 21, CurrentStrategy = 3 };
            var service = new GameStateService(data, ddaRankConfig: dda);
            service.MarkDdaToolOrReviveUsed();

            service.OnLevelWon(21);

            Assert.That(data.CurrentStrategy, Is.EqualTo(2));
            Assert.That(data.PreCatPendingDemote, Is.True);
            Assert.That(service.WasDdaToolOrReviveUsed, Is.False);
        }

        [Test]
        public void PlatformProgress_MatchesSourceCountersCooldownAndAttGuide()
        {
            var store = new CountingStore();
            var data = new GameStateData
            {
                TodayDate = "2026-08-11",
                RecentWinCountsByDay = new Dictionary<string, object>
                {
                    { "2026-08-09", 7 },
                    { "2026-08-10", 6 },
                    { "2026-08-11", 8 }
                }
            };
            var service = new GameStateService(
                data,
                store,
                dateProvider: new DateProvider("2026-08-11"));

            Assert.That(service.IsPushGuideCooldownElapsed(), Is.True);
            Assert.That(service.GetRecentThreeDayWinCount(), Is.EqualTo(21));

            service.IncrementPushAskCount();
            service.MarkPushGuideTriggered();
            service.MarkPushGuidePopupShown();
            service.MarkAttGuideShown();
            service.MarkAttGuideShown();

            Assert.That(service.PushAskCount, Is.EqualTo(1));
            Assert.That(service.PushGuideLastDate, Is.EqualTo("2026-08-11"));
            Assert.That(service.PushGuideShownCount, Is.EqualTo(1));
            Assert.That(service.PushGuidePopupCount, Is.EqualTo(1));
            Assert.That(service.HasShownAttGuide, Is.True);
            Assert.That(service.IsPushGuideCooldownElapsed(), Is.False);
            Assert.That(store.SaveCount, Is.EqualTo(4));
        }

        [Test]
        public void PushGuideCooldown_UsesSourceFiveCalendarDayBoundary()
        {
            var data = new GameStateData
            {
                PushGuideLastDate = "2026-08-06"
            };
            var service = new GameStateService(
                data,
                dateProvider: new DateProvider("2026-08-11"));

            Assert.That(service.IsPushGuideCooldownElapsed(), Is.True);
        }

        private sealed class CountingStore : IGameStatePlayerStore
        {
            public int SaveCount { get; private set; }
            public GameStateData LastData { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                SaveCount++;
                LastData = data;
                return true;
            }
        }

        private sealed class DateProvider : ICurrentDateProvider
        {
            public DateProvider(string date) { CurrentDate = date; }
            public string CurrentDate { get; }
        }

        private sealed class RecordingVibrationSink : IVibrationStateSink
        {
            public bool LastEnabled { get; private set; }
            public int CallCount { get; private set; }

            public void SetEnabled(bool enabled)
            {
                LastEnabled = enabled;
                CallCount++;
            }
        }

        private sealed class CombinedStore : IGameStatePlayerStore, IGameStateEndgameStore
        {
            public int PlayerSaveCount { get; private set; }
            public int ImmediateEndgameSaveCount { get; private set; }
            public int RequestedEndgameSaveCount { get; private set; }

            public bool SavePlayer(GameStateData data)
            {
                PlayerSaveCount++;
                return true;
            }

            public bool SaveEndgame(GameStateData data)
            {
                ImmediateEndgameSaveCount++;
                return true;
            }

            public bool RequestSaveEndgame(GameStateData data)
            {
                RequestedEndgameSaveCount++;
                return true;
            }

            public void ClearEndgame() { }
        }
    }
}

``

## PATH: Assets/_Project/Tests/EditMode/TrackingCoreTests.cs
``csharp
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Tracking;
using NUnit.Framework;

namespace Meowdoku.Tests.EditMode
{
    public sealed class TrackingCoreTests
    {
        [Test]
        public void SourceStack_FollowsScreenDialogButtonAndCloseRules()
        {
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                State(),
                sink,
                new SequentialIds());

            tracker.TrackScreenShown(TrackerCatalog.Screen.Home);
            tracker.TrackDialogShown(TrackerCatalog.Dialog.Settings);
            tracker.TrackButtonClick("close");

            Assert.That(sink.Events[0].Name,
                Is.EqualTo(TrackerCatalog.Event.ScreenShow));
            Assert.That(sink.Events[1].Parameters["source"],
                Is.EqualTo(TrackerCatalog.Screen.Home));
            Assert.That(sink.Events[2].Parameters["source"],
                Is.EqualTo(TrackerCatalog.Dialog.Settings));
            tracker.NotifyDialogClosed(TrackerCatalog.Dialog.Settings);
            Assert.That(tracker.CurrentSource,
                Is.EqualTo(TrackerCatalog.Screen.Home));
        }

        [Test]
        public void GameIdAndRoundStats_PersistAndRestartMatchesSource()
        {
            GameStateService state = State();
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                state,
                sink,
                new SequentialIds());
            Assert.That(
                tracker.NewGameId(TrackerCatalog.GameType.Normal),
                Is.EqualTo("id-1"));
            tracker.IncrementStat("hint_used", 2);
            tracker.IncrementStat("custom_total", 3);
            tracker.OnRestart();
            tracker.TrackGameEnd(new Dictionary<string, object>
            {
                ["result"] = TrackerCatalog.GameResult.Quit
            });

            Assert.That(tracker.GetStat("hint_used"), Is.Zero);
            Assert.That(tracker.GetStat("custom_total"), Is.EqualTo(3));
            Assert.That(tracker.GetStat("restart_count"), Is.EqualTo(1));
            Assert.That(sink.Events[0].Parameters["game_id"],
                Is.EqualTo("id-1"));
            Assert.That(state.GetPersistedGameId("normal"),
                Is.EqualTo("id-1"));
        }

        [Test]
        public void QuestionRotation_MatchesGodotTransformEncoding()
        {
            string[] expected =
            {
                "0", "90", "180", "270",
                "H0", "H90", "H180", "H270",
                "V0", "V90", "V180", "V270"
            };
            for (int index = 0; index < expected.Length; index++)
                Assert.That(
                    TrackerService.TransformToQuestionRotation(index),
                    Is.EqualTo(expected[index]));
        }

        [Test]
        public void Session_FlushesActiveTimeAndRefreshesOnlyAfterThirtyMinutes()
        {
            GameStateService state = State();
            var clock = new FakeClock
            {
                UnixNow = 1000,
                MonotonicMilliseconds = 5000
            };
            var session = new SessionService(
                state,
                true,
                clock,
                new SequentialIds());
            Assert.That(session.SessionId, Is.EqualTo("id-1"));
            Assert.That(state.Data.SessionCount, Is.EqualTo(1));

            clock.MonotonicMilliseconds += 65000;
            Assert.That(session.FlushActiveSegment(), Is.EqualTo(65));
            Assert.That(state.Data.TodayActiveSeconds, Is.EqualTo(65));
            Assert.That(state.Data.TotalActiveSeconds, Is.EqualTo(65));

            session.OnFocusOut();
            clock.UnixNow += 600;
            Assert.That(session.OnFocusIn(), Is.False);
            Assert.That(session.SessionRecord, Is.EqualTo(2));

            session.OnFocusOut();
            clock.UnixNow +=
                SessionService.SessionRefreshIntervalSeconds + 1;
            Assert.That(session.OnFocusIn(), Is.True);
            Assert.That(session.SessionId, Is.EqualTo("id-2"));
            Assert.That(session.SessionRecord, Is.EqualTo(1));
            Assert.That(state.Data.SessionCount, Is.EqualTo(2));
        }

        [Test]
        public void GrtMilestones_RoundTripWithoutDuplicates()
        {
            GameStateService state = State();
            state.MarkGrtLevelD90Reported(10);
            state.MarkGrtLevelD90Reported(10);
            state.MarkGrtEventReported("grt_level6_d0");
            state.MarkGrtEventReported("grt_level6_d0");

            Dictionary<string, object> document =
                state.Data.ToPlayerDocument();
            GameStateData restored =
                GameStateData.FromDocuments(document, null);

            Assert.That(restored.GrtLevelD90Reported,
                Is.EqualTo(new[] { 10 }));
            Assert.That(restored.GrtReportedEvents,
                Is.EqualTo(new[] { "grt_level6_d0" }));
        }

        [Test]
        public void AdAndPropEvents_PreserveExactSourcePayload()
        {
            var sink = new RecordingSink();
            var tracker = new TrackerService(
                State(),
                sink,
                new SequentialIds());

            tracker.TrackProp(
                false,
                TrackerCatalog.Prop.Hint,
                TrackerCatalog.Screen.NormalGame,
                1,
                4);
            tracker.TrackAdShowTiming(
                "show-1",
                TrackerCatalog.Placement.Reward,
                TrackerCatalog.Placement.Reward,
                TrackerCatalog.AdPosition.PropsNormalHint);
            tracker.TrackRewardedAdShow(
                "show-1",
                12,
                TrackerCatalog.AdPosition.PropsNormalHint);
            tracker.RememberAdShowId(
                TrackerCatalog.Placement.Reward,
                "show-1");

            Assert.That(sink.Events[0].Name,
                Is.EqualTo(TrackerCatalog.Event.PropUse));
            Assert.That(sink.Events[0].Parameters["prop_name"],
                Is.EqualTo("hint"));
            Assert.That(sink.Events[0].Parameters["prop_left"],
                Is.EqualTo(4));
            Assert.That(sink.Events[1].Name,
                Is.EqualTo(TrackerCatalog.Event.AdShowTiming));
            Assert.That(sink.Events[1].Parameters["position"],
                Is.EqualTo("props_normal_hint"));
            Assert.That(sink.Events[2].Name,
                Is.EqualTo(TrackerCatalog.Event.RewardedAdShow));
            Assert.That(sink.Events[2].Parameters["level"],
                Is.EqualTo(12));
            Assert.That(
                tracker.ConsumeAdShowId(TrackerCatalog.Placement.Reward),
                Is.EqualTo("show-1"));
            Assert.That(
                tracker.ConsumeAdShowId(TrackerCatalog.Placement.Reward),
                Is.Empty);
        }

        private static GameStateService State() =>
            new(new GameStateData(), new MemoryStore());

        private sealed class MemoryStore : IGameStatePlayerStore
        {
            public bool SavePlayer(GameStateData data) => true;
        }

        private sealed class SequentialIds : ITrackingIdProvider
        {
            private int _value;
            public string NewId() => $"id-{++_value}";
        }

        private sealed class FakeClock : ITrackingClock
        {
            public long UnixNow { get; set; }
            public long MonotonicMilliseconds { get; set; }
        }

        private sealed class RecordingSink : ITrackingSink
        {
            public readonly List<Entry> Events = new();
            public void SendEvent(
                string eventName,
                IReadOnlyDictionary<string, object> parameters)
            {
                Events.Add(new Entry(
                    eventName,
                    new Dictionary<string, object>(parameters)));
            }
            public void SetUserProperty(string name, string value) { }
        }

        private sealed class Entry
        {
            public Entry(
                string name,
                Dictionary<string, object> parameters)
            {
                Name = name;
                Parameters = parameters;
            }
            public string Name { get; }
            public Dictionary<string, object> Parameters { get; }
        }
    }
}

``

## PATH: Assets/_Project/Tests/PlayMode/PrimaryNavigationPlayModeTests.cs
``csharp
using System;
using System.Collections;
using System.Collections.Generic;
using Meowdoku.Core;
using Meowdoku.Core.Ads;
using Meowdoku.Core.Config;
using Meowdoku.Core.Daily;
using Meowdoku.Core.Platform;
using Meowdoku.Core.Profile;
using Meowdoku.Core.Rank;
using Meowdoku.Core.Robot;
using Meowdoku.Core.Tracking;
using Meowdoku.Core.UI;
using Meowdoku.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Meowdoku.Tests.PlayMode
{
    public sealed class PrimaryNavigationPlayModeTests
    {
        private const string AppScenePath =
            "Assets/_Project/Scenes/AppScene.unity";
        private const float StartupTimeoutSeconds = 12f;
        private const float TransitionTimeoutSeconds = 4f;

        private enum BankUiFlow
        {
            SizeThenTier,
            LevelRows
        }

        private sealed class RecordingSettingsExternalServices :
            ISettingsExternalServices
        {
            public bool IsOnline { get; set; }
            public bool IsConsentManagementRequired { get; set; }
            public int FeedbackOpenCount { get; private set; }
            public int ConsentOpenCount { get; private set; }
            public List<string> OpenedUrls { get; } = new();

            public void OpenFeedbackFaq()
            {
                FeedbackOpenCount++;
            }

            public void ShowConsentManagement()
            {
                ConsentOpenCount++;
            }

            public void OpenLocalizedPrivacyUrl(string defaultUrl)
            {
                OpenedUrls.Add(defaultUrl);
            }
        }

        private sealed class MutableSystemClock : ISystemClock
        {
            public DateTime LocalNow { get; set; }
            public double UnixSeconds { get; set; }
        }

        private sealed class MutableCurrentDate : ICurrentDateProvider
        {
            public MutableCurrentDate(string value)
            {
                CurrentDate = value;
            }

            public string CurrentDate { get; set; }
        }

        private sealed class MutableRobotTime : IRobotTimeProvider
        {
            public long UnixNow { get; set; }
        }

        private sealed class RankEnvironment : IRankActivityEnvironment
        {
            public bool LeaderboardEnabled { get; set; } = true;
            public int LeaderboardGroup { get; set; } =
                RankActivityConfig.GroupCats;
            public int CurrentLevel { get; set; } =
                RankActivityConfig.UnlockLevel;
        }

        private sealed class MemoryRankActivityStore : IRankActivityStore
        {
            public RankActivityData Current { get; private set; } = new();

            public RankActivityData Load() => Current;

            public bool Save(RankActivityData data)
            {
                Current = data;
                return true;
            }

            public void Reset()
            {
                Current = new RankActivityData();
            }
        }

        private sealed class MemoryRobotPoolStore : IRobotPoolStore
        {
            private readonly Dictionary<string, RobotPool> _pools = new();

            public IReadOnlyDictionary<string, RobotPool> LoadAll() => _pools;

            public bool SaveAll(IReadOnlyDictionary<string, RobotPool> pools)
            {
                _pools.Clear();
                foreach (KeyValuePair<string, RobotPool> pair in pools)
                    _pools[pair.Key] = pair.Value;
                return true;
            }

            public void Reset()
            {
                _pools.Clear();
            }

            public void ZeroAllScores()
            {
                foreach (RobotPool pool in _pools.Values)
                {
                    for (int index = 0; index < pool.Robots.Count; index++)
                    {
                        RobotData robot = pool.Robots[index];
                        robot.FinalScore = 0;
                        robot.Timeline.Clear();
                    }
                }
            }
        }

        private sealed class MemoryProfileDataStore : IProfileDataStore
        {
            private ProfileData _data = new();

            public ProfileData Load() => _data;

            public bool Save(ProfileData data)
            {
                _data = data;
                return true;
            }

            public void Reset()
            {
                _data = new ProfileData();
            }
        }

        private IDisposable _stateOverride;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            var data = new GameStateData
            {
                CurrentLevel = 1,
                IsFirstSession = false,
                TutorialDone = true,
                LastSplashDate = DateTime.Now.ToString("yyyy-MM-dd"),
                MaxDailyDate = DateTime.Now.ToString("yyyy-MM-dd")
            };
            _stateOverride = GameStateRuntime.OverrideForTests(
                new GameStateService(data));

            AsyncOperation load = SceneManager.LoadSceneAsync(
                AppScenePath,
                LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, "AppScene could not be loaded.");
            yield return load;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Scene appScene = SceneManager.GetSceneByPath(AppScenePath);
            if (appScene.IsValid() && appScene.isLoaded)
            {
                Scene cleanup = SceneManager.CreateScene(
                    "MeowdokuPlayModeCleanup");
                SceneManager.SetActiveScene(cleanup);
                AsyncOperation unload = SceneManager.UnloadSceneAsync(appScene);
                if (unload != null) yield return unload;
            }

            _stateOverride?.Dispose();
            _stateOverride = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator AppScene_PrimaryRoutes_OpenCloseAndReuseAtRuntime()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null,
                "AppScene is missing AppBootstrap.");
            Assert.That(manager, Is.Not.Null,
                "AppScene is missing UIManager.");

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(
                bootstrap.Phase,
                Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            yield return WaitForState(manager, UiName.Splash,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);

            UiName[] standaloneRoutes =
            {
                UiName.Tutorial,
                UiName.Language,
                UiName.HowToPlay,
                UiName.HowToPlayPaged,
                UiName.Bank
            };
            foreach (UiName route in standaloneRoutes)
                yield return ShowThenHide(manager, route);

            UIFrameWindow settings = manager.Show(UiName.Setting);
            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(manager.RequestBack(), Is.True,
                "Settings should consume the runtime back request.");
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            UIFrameWindow language = manager.Show(UiName.Language);
            Assert.That(language, Is.Not.Null);
            manager.Hide(UiName.Language);
            Assert.That(language.WindowState, Is.EqualTo(UiWindowState.Closing));
            UIFrameWindow reopened = manager.Show(UiName.Language);
            Assert.That(reopened, Is.SameAs(language),
                "Reopening a closing page must reuse the cached instance.");
            Assert.That(reopened.WindowState, Is.EqualTo(UiWindowState.Showing));
            yield return null;
            Assert.That(reopened.WindowState, Is.EqualTo(UiWindowState.Showing));
            manager.Hide(UiName.Language);
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Hidden);

            var parameters = new Dictionary<string, object>(1)
            {
                ["level_index"] = 1
            };
            UIFrameWindow game = manager.Show(UiName.Game, parameters);
            Assert.That(game, Is.Not.Null);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null,
                "GamePage is missing GameplayManager at runtime.");
            yield return WaitUntil(
                () => gameplay.CurrentPuzzleSize > 0,
                TransitionTimeoutSeconds,
                "Gameplay did not build its level after GamePage opened.");
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(4));
            manager.Hide(UiName.Game);
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);

            AssertShowing(manager, UiName.Home);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator PlatformStartup_PrivacyPushAndDailyNotifications()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsPrivacyRequiredValue = true;
            provider.IsMobileValue = true;
            provider.IsNotificationPermissionEnabledValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.AwaitPrivacyAndPush(),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.Privacy, UiWindowState.Showing);
            FindButton(manager.Get(UiName.Privacy), "AcceptButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                TransitionTimeoutSeconds,
                "Privacy/startup push flow did not complete.");
            yield return WaitForState(
                manager, UiName.Privacy, UiWindowState.Hidden);
            yield return WaitUntil(
                () => provider.SavedNotifications.Count == 2,
                TransitionTimeoutSeconds,
                "Daily local notifications were not registered.");

            Assert.That(provider.AgreePrivacyCount, Is.EqualTo(1));
            Assert.That(provider.InitializeTrackingCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestType,
                Is.EqualTo(NotificationPermissionRequestType.System));
            Assert.That(provider.NotificationPosition,
                Is.EqualTo("app_start"));
            Assert.That(provider.PushEnabled, Is.True);
            Assert.That(provider.RemovedNotificationIds,
                Is.EquivalentTo(new[] { "daily_noon", "daily_evening" }));
            Assert.That(GameStateRuntime.Current.PushAskCount, Is.EqualTo(1));
            Assert.That(GameStateRuntime.Current.PushGuideShownCount,
                Is.EqualTo(1));
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator PlatformAtt_CustomGuideContinuesBeforeSystemRequest()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsIosValue = true;
            provider.CanShowTrackingAuthorizationValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.AwaitConsentAndTracking(2f),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.PreAttGuide, UiWindowState.Showing);
            Assert.That(provider.TrackingRequestCount, Is.Zero,
                "System ATT must wait for the custom guide.");
            FindButton(manager.Get(UiName.PreAttGuide), "ContinueButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                5f,
                "ATT flow did not complete after Continue.");
            yield return WaitForState(
                manager, UiName.PreAttGuide, UiWindowState.Hidden);

            Assert.That(provider.ConsentCheckCount, Is.EqualTo(1));
            Assert.That(provider.TrackingRequestCount, Is.EqualTo(1));
            Assert.That(provider.TrackingSource, Is.EqualTo("splash_scr"));
            Assert.That(GameStateRuntime.Current.HasShownAttGuide, Is.True);
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator PlatformPushGuide_AllowUsesSourceRequestAndCounters()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            GameStateService state = GameStateRuntime.Current;
            state.Data.TodayDate = today;
            state.Data.RecentWinCountsByDay = new Dictionary<string, object>
            {
                [today] = 20
            };
            abRuntime.Platform.PushPermission.SetDebugOverride(
                PushPermissionConfig.ValueThreeDayProgress);

            PrivacyPermissionRuntime runtime = CreatePlatformRuntime(
                manager,
                abRuntime,
                out PlayModePlatformPermissionProvider provider);
            provider.IsMobileValue = true;
            provider.IsNotificationPermissionEnabledValue = true;
            bool completed = false;
            runtime.StartCoroutine(CompleteWhenDone(
                runtime.TryShowPushGuide(20),
                () => completed = true));

            yield return WaitForState(
                manager, UiName.PrePushGuide, UiWindowState.Showing);
            FindButton(manager.Get(UiName.PrePushGuide), "AllowButton")
                .onClick.Invoke();
            yield return WaitUntil(
                () => completed,
                TransitionTimeoutSeconds,
                "Push guide Allow flow did not complete.");
            yield return WaitForState(
                manager, UiName.PrePushGuide, UiWindowState.Hidden);

            Assert.That(provider.NotificationRequestCount, Is.EqualTo(1));
            Assert.That(provider.NotificationRequestType,
                Is.EqualTo(
                    NotificationPermissionRequestType.SystemAndSetting));
            Assert.That(provider.NotificationPosition,
                Is.EqualTo("push_guide"));
            Assert.That(state.PushAskCount, Is.EqualTo(1));
            Assert.That(state.PushGuidePopupCount, Is.EqualTo(1));
            Assert.That(state.PushGuideShownCount, Is.EqualTo(1));
            Assert.That(state.PushGuideLastDate, Is.EqualTo(today));
            Object.Destroy(runtime.gameObject);
        }

        [UnityTest]
        public IEnumerator AppScene_PrimaryButtons_NavigateSettingsAndGame()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            Assert.That(home, Is.Not.Null);
            AssertShowing(manager, UiName.Home);

            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            UIFrameWindow settings = manager.Get(UiName.Setting);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);

            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitUntil(
                () => gameplay.CurrentPuzzleSize > 0,
                TransitionTimeoutSeconds,
                "StartBtn opened Game without building a puzzle.");
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Hidden);

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_TransitionAndInputGuards_SurviveStress()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            AssertShowing(manager, UiName.Home);
            int baselineMaskCount = manager.MaskReferenceCount;
            int settingShown = 0;
            int settingHidden = 0;
            void CountSettingShown(UiName name, UIFrameWindow _) =>
                settingShown += name == UiName.Setting ? 1 : 0;
            void CountSettingHidden(UiName name, UIFrameWindow _) =>
                settingHidden += name == UiName.Setting ? 1 : 0;
            manager.Events.WindowShown += CountSettingShown;
            manager.Events.WindowHidden += CountSettingHidden;

            Button settingsButton = FindButton(home, "SettingsBtn");
            Assert.That(
                settingsButton.GetComponent<UIButtonPressGuard>(),
                Is.Not.Null,
                "Static Home buttons must own the source release guard.");
            ClickThroughPointerPhases(settingsButton);

            UIFrameWindow setting = manager.Get(UiName.Setting);
            Assert.That(setting, Is.Not.Null);
            Assert.That(setting.WindowState, Is.EqualTo(UiWindowState.Showing));
            Assert.That(manager.IsInputGuardActive, Is.True,
                "The release frame that opened Settings was not guarded.");
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(manager.IsInputGuardActive, Is.False,
                "Release-frame guard did not clean itself up.");

            int expectedMaskCount = baselineMaskCount +
                                    (setting.ShowMask ? 1 : 0);
            Assert.That(manager.MaskReferenceCount,
                Is.EqualTo(expectedMaskCount));

            RectTransform settingRect = setting.transform as RectTransform;
            manager.BlockInputBriefly(settingRect, 0.25f);
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True);
            AssertLocalInputBlocker(settingRect);
            yield return new WaitForSecondsRealtime(0.1f);
            manager.BlockInputBriefly(settingRect, 0.25f);
            yield return null;
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True,
                "Refreshing a timed blocker must replace, not cancel, it.");
            AssertLocalInputBlocker(settingRect);
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(manager.IsInputBrieflyBlocked(settingRect), Is.True,
                "The refreshed blocker expired on the old deadline.");
            yield return WaitUntil(
                () => !manager.IsInputBrieflyBlocked(settingRect),
                1f,
                "Timed local input blocker did not clean itself up.");

            for (int iteration = 0; iteration < 96; iteration++)
            {
                manager.Hide(UiName.Setting);
                Assert.That(setting.WindowState,
                    Is.EqualTo(UiWindowState.Closing));
                manager.Hide(UiName.Setting);
                UIFrameWindow reopened = manager.Show(UiName.Setting);
                Assert.That(reopened, Is.SameAs(setting));
                Assert.That(reopened.WindowState,
                    Is.EqualTo(UiWindowState.Showing));
                Assert.That(manager.MaskReferenceCount,
                    Is.EqualTo(expectedMaskCount));
                yield return null;
                Assert.That(reopened.WindowState,
                    Is.EqualTo(UiWindowState.Showing));
            }

            Assert.That(settingShown, Is.EqualTo(1),
                "Aborted closes must not emit duplicate shown events.");
            Assert.That(settingHidden, Is.Zero,
                "Aborted closes must not emit hidden events.");
            Assert.That(setting.SortingOrder,
                Is.GreaterThanOrEqualTo((int)setting.Layer));
            Assert.That(setting.SortingOrder,
                Is.LessThan((int)setting.Layer + UiLayerConfig.ZMax),
                "Sorting order did not compact inside the source Z range.");

            manager.Hide(UiName.Setting);
            yield return WaitForState(
                manager,
                UiName.Setting,
                UiWindowState.Hidden);
            Assert.That(settingHidden, Is.EqualTo(1));
            Assert.That(manager.MaskReferenceCount,
                Is.EqualTo(baselineMaskCount));
            manager.Events.WindowShown -= CountSettingShown;
            manager.Events.WindowHidden -= CountSettingHidden;

            bool languageWasCached = manager.Has(UiName.Language);
            int languageCreated = 0;
            int languageShown = 0;
            void CountLanguageCreated(UiName name, UIFrameWindow _) =>
                languageCreated += name == UiName.Language ? 1 : 0;
            void CountLanguageShown(UiName name, UIFrameWindow _) =>
                languageShown += name == UiName.Language ? 1 : 0;
            manager.Events.WindowCreated += CountLanguageCreated;
            manager.Events.WindowShown += CountLanguageShown;
            UIFrameWindow first = null;
            UIFrameWindow second = null;
            manager.StartCoroutine(manager.ShowAsync(
                UiName.Language,
                completed: window => first = window));
            manager.StartCoroutine(manager.ShowAsync(
                UiName.Language,
                completed: window => second = window));
            yield return WaitUntil(
                () => first != null && second != null,
                TransitionTimeoutSeconds,
                "Concurrent ShowAsync calls did not settle.");
            Assert.That(first, Is.SameAs(second));
            Assert.That(languageCreated,
                Is.EqualTo(languageWasCached ? 0 : 1),
                "One-flight loading created duplicate Language pages.");
            Assert.That(languageShown, Is.EqualTo(1),
                "One-flight loading emitted duplicate shown events.");
            Assert.That(manager.IsAnyLoading, Is.False);
            manager.Events.WindowCreated -= CountLanguageCreated;
            manager.Events.WindowShown -= CountLanguageShown;
            manager.Hide(UiName.Language);
            yield return WaitForState(
                manager,
                UiName.Language,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);
        }

        [UnityTest]
        public IEnumerator AppScene_SettingsConditionalButtons_UseSharedAbRuntimeAndSourceRoutes()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null,
                "AppScene is missing AbConfigRuntime.");
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "settings_language",
                SettingsLanguageConfig.ValuePopup);
            provider.SetInt(
                "rule_text",
                RuleTextConfig.ValueSettingEntry);
            provider.SetInt(
                "blind_mod",
                BlindModConfig.ValueHideOnFilled);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            UIFrameWindow settings = manager.Get(UiName.Setting);
            Button language = FindButton(settings, "LanguageBtn");
            Button homeHowToPlay = FindButton(
                settings,
                "HowToPlayBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(homeHowToPlay.gameObject.activeInHierarchy, Is.False,
                "HowToPlayBtn belongs only to game-mode Settings.");

            language.onClick.Invoke();
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Showing);
            AssertShowing(manager, UiName.Setting);
            manager.Hide(UiName.Language);
            yield return WaitForState(manager, UiName.Language,
                UiWindowState.Hidden);
            FindButton(settings, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);

            provider.SetInt(
                "settings_language",
                SettingsLanguageConfig.ValueDropdown);
            var settingsExternal = new RecordingSettingsExternalServices
            {
                IsConsentManagementRequired = true,
                IsOnline = false
            };
            manager.BindSettingsExternalServices(settingsExternal);
            SettingsPagePresenter settingsPresenter =
                settings as SettingsPagePresenter;
            Assert.That(settingsPresenter, Is.Not.Null);
            settingsPresenter.OverrideSystemLocaleForTests("vi_VN");

            FindButton(home, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            settings = manager.Get(UiName.Setting);
            LanguageSwitchWidget languageWidget =
                settings.GetComponentInChildren<LanguageSwitchWidget>(true);
            Assert.That(languageWidget, Is.Not.Null);
            Assert.That(languageWidget.gameObject.activeInHierarchy, Is.True,
                "Non-English system locale must expose dropdown mode.");
            language = FindButton(
                settings,
                "LanguageBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(language.gameObject.activeInHierarchy, Is.False);

            FindButton(settings, "FeedbackBtn").onClick.Invoke();
            Assert.That(settingsExternal.FeedbackOpenCount, Is.Zero,
                "Offline Feedback must stop at the source network gate.");
            settingsExternal.IsOnline = true;
            FindButton(settings, "FeedbackBtn").onClick.Invoke();
            Assert.That(settingsExternal.FeedbackOpenCount, Is.EqualTo(1));
            FindButton(settings, "PrivacyPreferenceBtn").onClick.Invoke();
            Assert.That(settingsExternal.ConsentOpenCount, Is.EqualTo(1));
            FindButton(settings, "TermsBtn").onClick.Invoke();
            FindButton(settings, "PrivacyBtn").onClick.Invoke();
            Assert.That(settingsExternal.OpenedUrls,
                Is.EqualTo(new[]
                {
                    "https://oakevergames.com/tos.html",
                    "https://oakevergames.com/pp.html"
                }));

            FindButton(settings, "Row").onClick.Invoke();
            Assert.That(languageWidget.IsOpen, Is.True);
            Graphic outside = FindNamedComponent<Graphic>(
                settings,
                "OutsideBlocker");
            Assert.That(outside.raycastTarget, Is.True);
            var outsidePress = new PointerEventData(EventSystem.current)
            {
                pointerCurrentRaycast = new RaycastResult
                {
                    gameObject = outside.gameObject
                }
            };
            languageWidget.OnPointerDown(outsidePress);
            Assert.That(languageWidget.IsOpen, Is.False,
                "Godot closes the dropdown on outside pointer-down.");
            AssertShowing(manager, UiName.Setting);

            FindButton(settings, "Row").onClick.Invoke();
            Assert.That(languageWidget.IsOpen, Is.True);
            FindButton(settings, "SystemLangOption").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            Assert.That(GameStateRuntime.Current.AppliedLocale,
                Is.EqualTo("vi_VN"));

            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            BoardView board = gameplay.boardView;
            Assert.That(board, Is.Not.Null);
            Assert.That(board.PatternOnForTests, Is.False);

            CellView patternCell = null;
            for (int row = 0; row < board.PuzzleSize && patternCell == null; row++)
            {
                for (int column = 0;
                     column < board.PuzzleSize && patternCell == null;
                     column++)
                {
                    if (board.GetCellState(row, column) == CellStateType.EMPTY)
                        patternCell = board.GetCellForTests(row, column);
                }
            }
            Assert.That(patternCell, Is.Not.Null,
                "The source level needs one empty cell for the pattern test.");
            Assert.That(patternCell.patternImage, Is.Not.Null,
                "Cell prefab did not serialize its Pattern image.");
            Assert.That(patternCell.IsPatternVisibleForTests, Is.False);

            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            Assert.That(GameStateRuntime.Current.PatternEntryDotDismissed,
                Is.True,
                "Opening game Settings must dismiss the source pattern-entry dot.");
            settings = manager.Get(UiName.Setting);
            language = FindButton(
                settings,
                "LanguageBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(language.gameObject.activeInHierarchy, Is.False,
                "Language entry must stay hidden in game-mode Settings.");

            bool soundBefore = GameStateRuntime.Current.SoundOn;
            FindButton(settings, "SoundBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.SoundOn, Is.EqualTo(!soundBefore));
            bool vibrationBefore = GameStateRuntime.Current.VibrationOn;
            FindButton(settings, "VibrationBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.VibrationOn,
                Is.EqualTo(!vibrationBefore));
            bool peopleBefore = GameStateRuntime.Current.PeopleOn;
            FindButton(settings, "PeopleBtn").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.PeopleOn, Is.EqualTo(!peopleBefore));

            FindButton(settings, "PatternModeSwitch").onClick.Invoke();
            Assert.That(GameStateRuntime.Current.PatternModeOn, Is.True);
            Assert.That(GameStateRuntime.Current.PatternSwitchDotDismissed,
                Is.True);
            Assert.That(board.PatternOnForTests, Is.True);
            Assert.That(board.PatternKeepOnFilledForTests, Is.False);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.True,
                "An empty cell must show its source pattern when enabled.");
            board.SetCellState(
                patternCell.Row,
                patternCell.Col,
                CellStateType.MARK,
                false);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.False,
                "blind_mod=1 must hide the pattern on a filled cell.");
            board.SetCellState(
                patternCell.Row,
                patternCell.Col,
                CellStateType.EMPTY,
                false);
            Assert.That(patternCell.IsPatternVisibleForTests, Is.True);

            int restartCount = gameplay.RestartCountForTests;
            RectTransform restartRow = FindNamedComponent<RectTransform>(
                settings,
                "OrangeRestartBtn");
            Button restart = FindOnlyButton(restartRow);
            restart.onClick.Invoke();
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.RestartCountForTests,
                Is.EqualTo(restartCount + 1),
                "Rapid Settings Restart presses must be consumed once.");

            FindButton(game, "SettingsBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Showing);
            settings = manager.Get(UiName.Setting);

            Button howToPlay = FindButton(settings, "HowToPlayBtn");
            howToPlay.onClick.Invoke();

            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Showing);
            yield return WaitForState(manager, UiName.Setting,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Game);
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Playing));

            HowToPlayPagedPagePresenter paged =
                manager.Get(UiName.HowToPlayPaged) as
                    HowToPlayPagedPagePresenter;
            Assert.That(paged, Is.Not.Null);
            Button main = FindButton(paged, "MainBtn");
            Button previous = FindButton(
                paged,
                "BackBtn",
                requireActive: false);
            Assert.That(paged.PageIndex, Is.EqualTo(0));
            main.onClick.Invoke();
            Assert.That(paged.PageIndex, Is.EqualTo(1));
            previous.onClick.Invoke();
            Assert.That(paged.PageIndex, Is.EqualTo(0));
            main.onClick.Invoke();
            main.onClick.Invoke();
            Assert.That(paged.PageIndex,
                Is.EqualTo(HowToPlayContract.PagedDemos.Count - 1));

            bool closedRaised = false;
            UiWindowState stateAtClosed = UiWindowState.Hidden;
            paged.Closed += () =>
            {
                closedRaised = true;
                stateAtClosed = paged.WindowState;
            };
            main.onClick.Invoke();
            Assert.That(closedRaised, Is.True,
                "Paged HTP must emit Closed from the user's close request.");
            Assert.That(stateAtClosed, Is.EqualTo(UiWindowState.Showing),
                "Godot emits closed before UIManager begins the close animation.");
            Assert.That(paged.WindowState, Is.EqualTo(UiWindowState.Closing));
            yield return WaitForState(manager, UiName.HowToPlayPaged,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Game);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_MainResultLoop_RestartWinAndContinue()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false,
                requireActive: false);
            Assert.That(revive.gameObject.activeInHierarchy, Is.False,
                "Offline default requires a rewarded ad, so Revive must stay hidden when the provider is unavailable.");
            Button restart = FindButton(fail, "RestartButton", false);
            yield return WaitUntil(
                () => restart.isActiveAndEnabled && restart.interactable,
                TransitionTimeoutSeconds,
                "RestartButton did not unlock after Fail appeared.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(1));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(GameStateRuntime.Current.CurrentLevel, Is.EqualTo(1));

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);
            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Win,
                UiWindowState.Hidden,
                15f);
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Continue did not load level 2 into the active Game page.");
            Assert.That(GameStateRuntime.Current.CurrentLevel, Is.EqualTo(2));
            AssertShowing(manager, UiName.Game);
        }

        [UnityTest]
        public IEnumerator AppScene_HomeFeatureEntries_FollowSharedAbAndNavigate()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null,
                "AppScene is missing AbConfigRuntime.");
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            provider.SetInt(
                "hard_button",
                HardButtonConfig.ValueDefault);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter presenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            DailyChallengeEntryPresenter daily =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            StreakEntryPresenter streak =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            RankActivityEntryPresenter rank =
                home.GetComponentInChildren<RankActivityEntryPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(daily, Is.Not.Null);
            Assert.That(streak, Is.Not.Null);
            Assert.That(rank, Is.Not.Null);
            Assert.That(
                FindNamedComponent<RectTransform>(home, "ProfileEntry")
                    .gameObject.activeInHierarchy,
                Is.True,
                "leaderboard_func must control the Profile entry like the source.");

            Button dailyButton = FindEntryButton(daily);
            Button streakButton = FindEntryButton(streak);
            Button rankButton = FindEntryButton(rank);
            Assert.That(dailyButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(streakButton.gameObject.activeInHierarchy, Is.True);
            Assert.That(rankButton.gameObject.activeInHierarchy, Is.False,
                "Rank entry must stay unavailable below source unlock level 11.");

            dailyButton.onClick.Invoke();
            yield return null;
            AssertShowing(manager, UiName.Home);
            Assert.That(manager.Has(UiName.DailyGame), Is.False,
                "Locked Daily entry must not create/open DailyGame.");

            streakButton.onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            FindButton(manager.Get(UiName.Streak), "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden);
            AssertShowing(manager, UiName.Home);

            GameStateRuntime.Current.Data.CurrentLevel = 21;
            presenter.RefreshPresentation();
            daily.RefreshNow();
            dailyButton = FindEntryButton(daily);
            dailyButton.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Showing);
            UIFrameWindow dailyGame = manager.Get(UiName.DailyGame);
            GameplayManager dailyGameplay =
                dailyGame.GetComponentInChildren<GameplayManager>(true);
            Assert.That(dailyGameplay, Is.Not.Null);
            yield return WaitForSession(dailyGameplay, GameSessionState.Playing);
            Assert.That(dailyGameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            FindButton(dailyGame, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => IsShowing(manager, UiName.RankActivityOpenPopup) ||
                      FindEntryButton(rank).gameObject.activeInHierarchy,
                TransitionTimeoutSeconds,
                "Rank entry did not become available at level 21.");
            if (!IsShowing(manager, UiName.RankActivityOpenPopup))
                FindEntryButton(rank).onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing);

            UIFrameWindow rankOpen = manager.Get(UiName.RankActivityOpenPopup);
            FindButton(rankOpen, "ActionButton").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            yield return WaitUntil(
                () => IsShowing(manager, UiName.Profile) ||
                      IsShowing(manager, UiName.Game),
                TransitionTimeoutSeconds,
                "Rank participation did not continue to Profile guide or Game.");
            if (IsShowing(manager, UiName.Profile))
            {
                FindButton(manager.Get(UiName.Profile), "CloseBtn")
                    .onClick.Invoke();
                yield return WaitForState(manager, UiName.Profile,
                    UiWindowState.Hidden);
            }
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            Assert.That(rankRuntime.Manager.IsJoined, Is.True);
        }

        [UnityTest]
        public IEnumerator AppScene_DailyEntries_RolloverOnResumeAndHonorMaxDate()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            ClockTicker ticker = Find<ClockTicker>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(ticker, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abRuntime.BindProvider(provider);

            var clock = new MutableSystemClock
            {
                LocalNow = new DateTime(2026, 8, 10, 23, 59, 59),
                UnixSeconds = 100.25
            };
            var date = new MutableCurrentDate("2026-08-10");
            ticker.ConfigureForTests(clock);
            var streakFeature = new StreakFeature(
                dateProvider: date,
                streakConfig: abRuntime.Home.DailyStreak,
                initialData: new StreakData
                {
                    CurrentStreak = 3,
                    BestStreak = 3,
                    RewardCycleDay = 3,
                    LastCheckinDate = "2026-08-10",
                    StreakStartWeekday = 1
                });
            dailyRuntime.ConfigureForTests(
                streakFeature,
                dailyRuntime.Awards);
            dailyRuntime.BindAbConfigRuntime(abRuntime);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            state.Data.DailyCompletedDate = "2026-08-10";
            state.Data.MaxDailyDate = "2026-08-09";

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            DailyChallengeEntryPresenter dailyEntry =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            StreakEntryPresenter streakEntry =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            Assert.That(dailyEntry, Is.Not.Null);
            Assert.That(streakEntry, Is.Not.Null);
            Assert.That(dailyEntry.StateForTests,
                Is.EqualTo(DailyEntryState.Done));
            Assert.That(streakEntry.IsCheckedForTests, Is.True);
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-10"),
                "Home show must advance max_daily_date with the shared clock.");

            clock.LocalNow = new DateTime(2026, 8, 11, 0, 0, 0);
            clock.UnixSeconds = 200.25;
            date.CurrentDate = "2026-08-11";
            ticker.SendMessage(
                "OnApplicationFocus",
                true,
                SendMessageOptions.RequireReceiver);
            yield return WaitUntil(
                () => dailyEntry.StateForTests == DailyEntryState.Normal &&
                      !streakEntry.IsCheckedForTests,
                2f,
                "Daily/Streak entries did not roll over after focus resumed.");
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-10"),
                "Live ticks must refresh entries without inventing a Home show.");
            Assert.That(streakFeature.DisplayStreak, Is.EqualTo(3),
                "Day-watch refresh must not mutate streak progress.");

            manager.Hide(UiName.Home);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Hidden);
            manager.Show(UiName.Home);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-11"),
                "Reopening Home must persist the newly observed local date.");

            state.Data.DailyCompletedDate = string.Empty;
            clock.LocalNow = new DateTime(2026, 8, 10, 12, 0, 0);
            clock.UnixSeconds = 300.25;
            date.CurrentDate = "2026-08-10";
            ticker.SendMessage(
                "OnApplicationPause",
                false,
                SendMessageOptions.RequireReceiver);
            yield return WaitUntil(
                () => dailyEntry.StateForTests == DailyEntryState.Done &&
                      streakEntry.IsCheckedForTests,
                2f,
                "Backdated local clock did not refresh through the pause hook.");
            Assert.That(state.MaxDailyDate, Is.EqualTo("2026-08-11"));
        }

        [UnityTest]
        public IEnumerator AppScene_Streak_MultiDayCycleRewardAndBrokenReset()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abRuntime.BindProvider(provider);

            var date = new MutableCurrentDate("2026-08-10");
            var protect = new StreakProtectConfig();
            protect.SetDebugOverride(StreakProtectConfig.ValueControl);
            AwardManager awards = dailyRuntime.Awards;
            var streak = new StreakFeature(
                dateProvider: date,
                streakConfig: abRuntime.Home.DailyStreak,
                protectConfig: protect,
                rewardBoundary: awards,
                initialData: new StreakData());
            dailyRuntime.ConfigureForTests(streak, awards);
            dailyRuntime.BindAbConfigRuntime(abRuntime);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            int initialHint = state.GetToolCount("hint");
            int initialLocate = state.GetToolCount("locate");

            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            StreakEntryPresenter entry =
                home.GetComponentInChildren<StreakEntryPresenter>(true);
            Assert.That(entry, Is.Not.Null);
            entry.RefreshNow();
            FindEntryButton(entry).onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            StreakPagePresenter page = manager.Get(UiName.Streak)
                .GetComponentInChildren<StreakPagePresenter>(true);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.StateForTests,
                Is.EqualTo(StreakDisplayState.Main));
            Assert.That(CountCheckedStreakSlots(page), Is.Zero);
            FindButton(manager.Get(UiName.Streak), "BackBtn")
                .onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden);

            for (int day = 1; day <= StreakFeature.CycleLength; day++)
            {
                date.CurrentDate = "2026-08-" +
                                   (9 + day).ToString("00");
                streak.TickDayWatch();
                dailyRuntime.SettleWin(StreakCheckinSource.Main);

                Assert.That(streak.Data.CurrentStreak, Is.EqualTo(day));
                Assert.That(streak.Data.BestStreak, Is.EqualTo(day));
                Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(day));
                Assert.That(streak.Data.LastCheckinDate,
                    Is.EqualTo(date.CurrentDate));
                Assert.That(CountCheckedWeekSlots(streak), Is.EqualTo(day));
                Assert.That(streak.PendingShowUid,
                    day == StreakFeature.CycleLength
                        ? Is.GreaterThan(0)
                        : Is.Zero);

                yield return PresentPendingStreak(
                    manager,
                    streak,
                    day - 1,
                    day);
                Assert.That(streak.HasPendingShow, Is.False);
            }

            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(state.GetInFlightAwards(), Is.Empty,
                "The seventh-day chest must complete its durable award.");

            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak,
                Is.EqualTo(StreakFeature.CycleLength));
            Assert.That(streak.HasPendingShow, Is.False,
                "A second win on the same local day must be idempotent.");
            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));

            date.CurrentDate = "2026-08-17";
            streak.TickDayWatch();
            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak, Is.EqualTo(8));
            Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(8));
            Assert.That(CountCheckedWeekSlots(streak), Is.EqualTo(1));
            yield return PresentPendingStreak(manager, streak, 0, 1);

            date.CurrentDate = "2026-08-19";
            streak.TickDayWatch();
            Assert.That(streak.IsBroken(), Is.True);
            Assert.That(streak.DisplayStreak, Is.Zero);
            dailyRuntime.SettleWin(StreakCheckinSource.Main);
            Assert.That(streak.Data.CurrentStreak, Is.EqualTo(1));
            Assert.That(streak.Data.BestStreak, Is.EqualTo(8));
            Assert.That(streak.Data.RewardCycleDay, Is.EqualTo(1));
            Assert.That(streak.Data.LastCheckinDate,
                Is.EqualTo("2026-08-19"));
            yield return PresentPendingStreak(manager, streak, 0, 1);

            Assert.That(state.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2),
                "Only the seventh-day chest may grant tools in this matrix.");
            Assert.That(state.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_RankFirstPeriod_CloseStillJoinsAndEntersMain()
        {
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            GameStateRuntime.Current.Data.CurrentLevel = 21;
            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(rankRuntime.Manager.MaybeOpen(true), Is.True);
            Assert.That(rankRuntime.Manager.PeriodCount, Is.EqualTo(1));
            Assert.That(rankRuntime.Manager.IsOpenNotJoined, Is.True);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter presenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            RankActivityEntryPresenter rankEntry =
                home.GetComponentInChildren<RankActivityEntryPresenter>(true);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(rankEntry, Is.Not.Null);
            presenter.RefreshPresentation();
            rankEntry.RefreshNow();
            yield return WaitUntil(
                () => IsShowing(manager, UiName.RankActivityOpenPopup) ||
                      FindEntryButton(rankEntry).gameObject.activeInHierarchy,
                TransitionTimeoutSeconds,
                "First-period Rank entry did not become available.");
            if (!IsShowing(manager, UiName.RankActivityOpenPopup))
                FindEntryButton(rankEntry).onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing);

            var popup = manager.Get(UiName.RankActivityOpenPopup) as
                RankActivityOpenPopupPresenter;
            Assert.That(popup, Is.Not.Null);
            Assert.That(popup.WasStarted, Is.False);
            FindButton(popup, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            Assert.That(popup.WasStarted, Is.False,
                "Close must remain distinct from the Play action.");
            yield return WaitUntil(
                () => IsShowing(manager, UiName.Profile) ||
                      IsShowing(manager, UiName.Game),
                TransitionTimeoutSeconds,
                "First-period Close did not continue through Profile or Game.");
            if (IsShowing(manager, UiName.Profile))
            {
                FindButton(manager.Get(UiName.Profile), "CloseBtn")
                    .onClick.Invoke();
                yield return WaitForState(manager, UiName.Profile,
                    UiWindowState.Hidden);
            }
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            Assert.That(rankRuntime.Manager.IsJoined, Is.True,
                "Godot confirms Rank participation after either popup exit.");
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.CurrentLevelNumber, Is.EqualTo(21));
        }

        [UnityTest]
        public IEnumerator AppScene_RankExpiryInGame_RewardThenOpensNextPeriodAtHome()
        {
            GameStateRuntime.Current.Data.CurrentLevel = 21;

            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            Assert.That(abRuntime, Is.Not.Null);
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);

            PlayModeAbProvider provider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            provider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(provider);

            var time = new MutableRobotTime { UnixNow = 2_000_000 };
            var robotStore = new MemoryRobotPoolStore();
            var robots = new RobotService(
                robotStore,
                time,
                new SystemRobotRandomFactory());
            var profile = new ProfileService(new MemoryProfileDataStore());
            var awards = new AwardManager(
                GameStateRuntime.Current,
                profile);
            dailyRuntime.ConfigureForTests(dailyRuntime.Streak, awards);
            var rank = new RankActivityManager(
                new MemoryRankActivityStore(),
                robots,
                profile,
                awards,
                new RankEnvironment(),
                time,
                new SystemRobotRandomFactory());
            rankRuntime.ConfigureForTests(rank);

            Assert.That(rank.MaybeOpen(true), Is.True);
            rank.ConfirmParticipation();
            robotStore.ZeroAllScores();
            Assert.That(rank.PeriodCount, Is.EqualTo(1));
            Assert.That(rank.IsJoined, Is.True);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(rank.IsInLevelForTests, Is.True);

            time.UnixNow += RankActivityConfig.PeriodDurationSeconds + 1;
            rank.Tick();
            Assert.That(rank.State, Is.EqualTo(RankActivityState.Settling));
            Assert.That(rank.GetPendingReward(), Is.Null,
                "Expiry during a level must defer settlement until win/exit.");
            Assert.That(rank.IsInLevelForTests, Is.True);

            int initialHint = GameStateRuntime.Current.GetToolCount("hint");
            int initialLocate = GameStateRuntime.Current.GetToolCount("locate");
            int initialFrame = profile.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId);
            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return WaitUntil(
                () => !rank.IsInLevelForTests &&
                      rank.GetPendingReward() != null,
                10f,
                "Rank settlement did not follow the win collection flight.");
            Assert.That(rank.IsInLevelForTests, Is.False);
            Assert.That(rank.GetPendingReward(), Is.Not.Null);
            Assert.That(rank.GetPendingReward().Rank, Is.EqualTo(1));

            yield return WaitForState(manager, UiName.RankActivityChange,
                UiWindowState.Showing,
                15f);
            UIFrameWindow change = manager.Get(UiName.RankActivityChange);
            Button tapToContinue = FindButton(
                change,
                "TapToContinue",
                requireInteractable: false,
                requireActive: false);
            yield return WaitUntil(
                () => tapToContinue.gameObject.activeInHierarchy &&
                      tapToContinue.interactable,
                8f,
                "Rank Change did not unlock Tap to Continue.");
            tapToContinue.onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityChange,
                UiWindowState.Hidden);

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing,
                10f);
            yield return CollectRankGift(manager);
            Assert.That(rank.State, Is.EqualTo(RankActivityState.NotOpened));
            Assert.That(rank.PeriodCount, Is.EqualTo(1),
                "An in-game reward must wait for Home before opening period 2.");
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint + 2));
            Assert.That(GameStateRuntime.Current.GetToolCount("locate"),
                Is.EqualTo(initialLocate + 2));
            Assert.That(profile.GetFrameCount(ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1));
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);

            yield return WaitForState(manager, UiName.Win,
                UiWindowState.Showing,
                15f);
            Button next = FindActiveButton(
                manager.Get(UiName.Win),
                "Next",
                requireInteractable: false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 22 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Next did not load level 22.");

            FindButton(game, "BackBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing,
                15f);
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Showing,
                15f);
            Assert.That(rank.PeriodCount, Is.EqualTo(2));
            Assert.That(rank.IsOpenNotJoined, Is.True);

            var periodTwoPopup = manager.Get(UiName.RankActivityOpenPopup) as
                RankActivityOpenPopupPresenter;
            Assert.That(periodTwoPopup, Is.Not.Null);
            Assert.That(periodTwoPopup.WasStarted, Is.False);
            FindButton(periodTwoPopup, "CloseBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.RankActivityOpenPopup,
                UiWindowState.Hidden);
            yield return WaitUntil(
                () => rank.IsJoined,
                TransitionTimeoutSeconds,
                "Period 2 participation was not confirmed after popup close.");
            Assert.That(rank.IsJoined, Is.True);
            Assert.That(rank.PeriodCount, Is.EqualTo(2));
            AssertShowing(manager, UiName.Home);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_RankFrameOnlyGift_UsesFrameEffectAndPersistsOnce()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            DailyMetaRuntime dailyRuntime = Find<DailyMetaRuntime>();
            ProfileRuntime profileRuntime = Find<ProfileRuntime>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            Assert.That(dailyRuntime, Is.Not.Null);
            Assert.That(profileRuntime, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            var awards = new AwardManager(
                GameStateRuntime.Current,
                profileRuntime);
            dailyRuntime.ConfigureForTests(dailyRuntime.Streak, awards);
            int initialFrame = profileRuntime.Service.GetFrameCount(
                ProfileCatalog.FirstPlaceFrameId);
            int initialHint = GameStateRuntime.Current.GetToolCount("hint");
            int uid = awards.Dispatch(
                new[]
                {
                    AwardItem.Frame(ProfileCatalog.FirstPlaceFrameId)
                },
                AwardDisplayType.RankGift,
                RankActivityManager.RewardReason);
            Assert.That(uid, Is.GreaterThan(0));
            Assert.That(awards.ShowAward(
                uid,
                new Dictionary<string, object>
                {
                    ["place"] = 1,
                    ["win_count"] = 1,
                    ["top3_infos"] = Array.Empty<object>()
                }), Is.True);

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Showing,
                10f);
            UIFrameWindow award = manager.Get(UiName.Award);
            FrameAwardEffectView effect =
                award.GetComponentInChildren<FrameAwardEffectView>(true);
            Assert.That(effect, Is.Not.Null);
            Button podiumCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => podiumCollect.interactable,
                5f,
                "Frame-only Rank Gift collect did not unlock.");
            podiumCollect.onClick.Invoke();
            yield return null;

            Assert.That(effect.gameObject.activeInHierarchy, Is.True);
            Assert.That(effect.IsPlaying, Is.True);
            Assert.That(effect.DisplayedFrameId,
                Is.EqualTo(ProfileCatalog.FirstPlaceFrameId));
            Assert.That(award.transform.Find("AwardPanel")
                .gameObject.activeInHierarchy, Is.False,
                "Frame-only phase must not expose the generic item panel.");
            Assert.That(profileRuntime.Service.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame),
                "The award must persist only after the frame effect ends.");

            yield return WaitForState(manager, UiName.Award,
                UiWindowState.Hidden,
                10f);
            Assert.That(profileRuntime.Service.GetFrameCount(
                    ProfileCatalog.FirstPlaceFrameId),
                Is.EqualTo(initialFrame + 1));
            Assert.That(GameStateRuntime.Current.GetToolCount("hint"),
                Is.EqualTo(initialHint));
            Assert.That(GameStateRuntime.Current.GetInFlightAwards(), Is.Empty);
            Assert.That(awards.ActiveRenderCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AppScene_DailyResultLoop_IsolatedReviveRestartWinAndReturnsToMain()
        {
            AdRuntime adRuntime = Find<AdRuntime>();
            AbConfigRuntime abRuntime = Find<AbConfigRuntime>();
            Assert.That(adRuntime, Is.Not.Null);
            Assert.That(abRuntime, Is.Not.Null);
            PlayModeAdProvider adProvider =
                adRuntime.gameObject.AddComponent<PlayModeAdProvider>();
            adRuntime.BindProvider(adProvider);
            PlayModeAbProvider abProvider =
                abRuntime.gameObject.AddComponent<PlayModeAbProvider>();
            abProvider.SetInt(
                "daily_streak",
                DailyStreakConfig.ValueBasic);
            abProvider.SetInt(
                "leaderboard_func",
                LeaderboardFuncConfig.ValueCatsProp);
            abRuntime.BindProvider(abProvider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);
            yield return WaitForState(manager, UiName.Home,
                UiWindowState.Showing);

            GameStateService state = GameStateRuntime.Current;
            state.Data.CurrentLevel = 21;
            state.Data.CurrentStrategy = 3;
            state.Data.ConsecutiveFails = 5;
            state.Data.RetryPuzzleLevel = 21;
            state.Data.RetryPuzzleParameters = new Dictionary<string, object>
            {
                ["sentinel"] = "main-retry"
            };
            state.Data.EndgameSnapshot = new Dictionary<string, object>
            {
                ["sentinel"] = "main-snapshot"
            };
            state.Data.MainGameTotalStats = new Dictionary<string, object>
            {
                ["sentinel"] = 7
            };

            RankActivityRuntime rankRuntime = Find<RankActivityRuntime>();
            Assert.That(rankRuntime, Is.Not.Null);
            Assert.That(rankRuntime.Manager.MaybeOpen(true), Is.True);
            rankRuntime.Manager.ConfirmParticipation();
            rankRuntime.Manager.SetLevelCollect(17);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);

            UIFrameWindow home = manager.Get(UiName.Home);
            HomePagePresenter homePresenter =
                home.GetComponentInChildren<HomePagePresenter>(true);
            DailyChallengeEntryPresenter dailyEntry =
                home.GetComponentInChildren<DailyChallengeEntryPresenter>(true);
            Assert.That(homePresenter, Is.Not.Null);
            Assert.That(dailyEntry, Is.Not.Null);
            homePresenter.RefreshPresentation();
            dailyEntry.RefreshNow();
            FindEntryButton(dailyEntry).onClick.Invoke();

            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Showing);
            UIFrameWindow dailyGame = manager.Get(UiName.DailyGame);
            GameplayManager gameplay =
                dailyGame.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            string dailyDate = gameplay.DailyDateForTests;
            int dailyIndex = gameplay.DailyIndexForTests;
            int dailySize = gameplay.CurrentPuzzleSize;
            var solution = new int[dailySize];
            for (int row = 0; row < dailySize; row++)
                solution[row] = gameplay.SolutionColumnForTests(row);
            Assert.That(dailyDate, Is.Not.Empty);
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False,
                "DailyGame must not enter the Main-only Rank level lifecycle.");
            AssertDailyDidNotMutateMainState(state);

            yield return FailCurrentSession(
                gameplay,
                manager,
                UiName.DailyFail,
                UiName.DailyGame);
            UIFrameWindow dailyFail = manager.Get(UiName.DailyFail);
            Button revive = FindButton(
                dailyFail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Daily rewarded ReviveButton did not unlock.");
            int showsBeforeRevive = adProvider.ShowCount;
            revive.onClick.Invoke();
            Assert.That(adProvider.ShowCount,
                Is.EqualTo(showsBeforeRevive + 1));
            Assert.That(adProvider.LastPosition,
                Is.EqualTo(TrackerCatalog.AdPosition.DailyGameFail));
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed));
            adProvider.EmitShown();
            adProvider.EmitRewarded();
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            adProvider.EmitClosed();
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("daily", "revive_count"),
                Is.EqualTo(1));
            Assert.That(state.GetGameTotalStat("main", "revive_count"),
                Is.Zero);
            AssertDailyDidNotMutateMainState(state);

            Vector2Int lastWrong = FindEmptyWrongCell(gameplay);
            SessionActionResult failed = gameplay.DoubleTapForTests(
                lastWrong.x,
                lastWrong.y);
            Assert.That(failed.Accepted, Is.True);
            Assert.That(failed.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(failed.LivesAfter, Is.Zero);
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Showing);
            dailyFail = manager.Get(UiName.DailyFail);
            Button restart = FindButton(
                dailyFail,
                "RestartButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => restart.interactable,
                TransitionTimeoutSeconds,
                "Daily RestartButton did not unlock.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyFail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Daily));
            Assert.That(gameplay.RestartCountForTests, Is.EqualTo(1));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(gameplay.DailyDateForTests, Is.EqualTo(dailyDate));
            Assert.That(gameplay.DailyIndexForTests, Is.EqualTo(dailyIndex));
            Assert.That(gameplay.CurrentPuzzleSize, Is.EqualTo(dailySize));
            for (int row = 0; row < dailySize; row++)
                Assert.That(gameplay.SolutionColumnForTests(row),
                    Is.EqualTo(solution[row]));
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);
            AssertDailyDidNotMutateMainState(state);

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager, UiName.DailyWin);
            UIFrameWindow dailyWin = manager.Get(UiName.DailyWin);
            Assert.That(gameplay.SessionState, Is.EqualTo(GameSessionState.Won));
            Assert.That(state.DailyCompletedDate, Is.EqualTo(dailyDate));
            Assert.That(state.DailyElapsedSeconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(state.DailyBeatPercent, Is.InRange(0f, 100f));
            Assert.That(rankRuntime.Manager.CollectTotal, Is.Zero,
                "Daily Win must not commit Main-only Rank collect.");
            Assert.That(rankRuntime.Manager.LevelCacheForTests, Is.EqualTo(17));
            Assert.That(rankRuntime.Manager.IsLevelCacheActiveForTests, Is.True);
            Assert.That(rankRuntime.Manager.IsInLevelForTests, Is.False);
            AssertDailyDidNotMutateMainState(state);

            Button dailyContinue = FindActiveButton(
                dailyWin,
                "Continue",
                requireInteractable: false);
            yield return WaitUntil(
                () => dailyContinue.interactable &&
                      !manager.IsInputBrieflyBlocked(
                          dailyWin.transform as RectTransform),
                3f,
                "Daily Continue did not unlock after the source 2 second gate.");
            dailyContinue.onClick.Invoke();
            yield return WaitForState(manager, UiName.DailyWin,
                UiWindowState.Hidden,
                15f);
            yield return WaitForState(manager, UiName.DailyGame,
                UiWindowState.Hidden,
                15f);
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing,
                15f);
            GameplayManager mainGameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(mainGameplay, Is.Not.Null);
            Assert.That(mainGameplay, Is.Not.SameAs(gameplay),
                "Daily Continue must open the real Main Game page.");
            yield return WaitForSession(mainGameplay, GameSessionState.Playing);
            Assert.That(mainGameplay.SessionMode,
                Is.EqualTo(GameplaySessionMode.Main));
            Assert.That(mainGameplay.CurrentLevelNumber, Is.EqualTo(21));
            Assert.That(state.CurrentLevel, Is.EqualTo(21),
                "Daily Win must not advance Main progression.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_LifecycleBoundary_PreservesPlayingFailReviveWinAndNextState()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            FindButton(manager.Get(UiName.Home), "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            GameplayManager gameplay = manager.Get(UiName.Game)
                .GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            Vector2Int markedCell = FindEmptyWrongCell(gameplay);
            Assert.That(
                gameplay.ApplyCellStateForTests(
                    markedCell.x,
                    markedCell.y,
                    CellStateType.MARK),
                Is.True);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "A MARK must remain debounced until its save deadline or a lifecycle boundary.");

            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> playingSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(playingSnapshot, Is.Not.Empty);
            Assert.That(playingSnapshot["level"], Is.EqualTo(1));
            Assert.That(((IList)playingSnapshot["marks"]).Count, Is.EqualTo(1));
            Assert.That(Convert.ToDouble(playingSnapshot["in_game_sec"]),
                Is.EqualTo(gameplay.SnapshotElapsedSecondsForTests).Within(0.05));

            gameplay.SuspendApplicationForTests();
            Assert.That(
                GameStateRuntime.Current.GetEndgameSnapshot(),
                Is.SameAs(playingSnapshot),
                "Focus-out plus pause must share one durability boundary.");
            gameplay.ResumeApplicationForTests();

            yield return FailCurrentSession(gameplay, manager);
            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> failedSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(failedSnapshot["lives"], Is.EqualTo(0));
            Assert.That(failedSnapshot["level"], Is.EqualTo(1));
            gameplay.ResumeApplicationForTests();

            Assert.That(gameplay.ReviveFromFail(1), Is.True);
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot()["lives"],
                Is.EqualTo(1));
            gameplay.SuspendApplicationForTests();
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot()["lives"],
                Is.EqualTo(1));
            gameplay.ResumeApplicationForTests();

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "Win settlement must clear the resumable snapshot.");
            gameplay.SuspendApplicationForTests();
            Assert.That(GameStateRuntime.Current.GetEndgameSnapshot(), Is.Empty,
                "Suspending on Win must not recreate a completed snapshot.");
            gameplay.ResumeApplicationForTests();

            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Win Next button did not become interactable.");
            next.onClick.Invoke();
            yield return WaitUntil(
                () => gameplay.CurrentLevelNumber == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Next did not load level 2.");

            Vector2Int levelTwoMark = FindEmptyWrongCell(gameplay);
            Assert.That(
                gameplay.ApplyCellStateForTests(
                    levelTwoMark.x,
                    levelTwoMark.y,
                    CellStateType.MARK),
                Is.True);
            gameplay.SuspendApplicationForTests();
            Dictionary<string, object> nextSnapshot =
                GameStateRuntime.Current.GetEndgameSnapshot();
            Assert.That(nextSnapshot["level"], Is.EqualTo(2));
            Assert.That(((IList)nextSnapshot["marks"]).Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppScene_FailRewardedRevive_RequiresRewardAndRecoversAfterCloseFailure()
        {
            AdRuntime adRuntime = Find<AdRuntime>();
            Assert.That(adRuntime, Is.Not.Null,
                "AppScene is missing AdRuntime.");
            PlayModeAdProvider provider =
                adRuntime.gameObject.AddComponent<PlayModeAdProvider>();
            adRuntime.BindProvider(provider);

            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow home = manager.Get(UiName.Home);
            FindButton(home, "StartBtn").onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Rewarded ReviveButton did not unlock.");

            revive.onClick.Invoke();
            Assert.That(provider.ShowCount, Is.EqualTo(1));
            Assert.That(provider.LastPlacementId,
                Is.EqualTo(TrackerCatalog.Placement.Reward));
            Assert.That(provider.LastPosition,
                Is.EqualTo(TrackerCatalog.AdPosition.NormalGameFail));
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed),
                "Opening a rewarded ad must not revive before its reward callback.");

            provider.EmitShown();
            provider.EmitRewarded();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));
            Assert.That(
                GameStateRuntime.Current.GetGameTotalStat(
                    "main",
                    "revive_count"),
                Is.EqualTo(1));

            provider.EmitClosed();
            yield return null;
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Playing),
                "Closing an already rewarded ad must not settle revive twice.");
            Assert.That(gameplay.LivesForTests, Is.EqualTo(1));

            Vector2Int wrongCell = FindEmptyWrongCell(gameplay);
            SessionActionResult failed = gameplay.DoubleTapForTests(
                wrongCell.x,
                wrongCell.y);
            Assert.That(failed.Accepted, Is.True);
            Assert.That(failed.Kind, Is.EqualTo(SessionActionKind.WrongGuess));
            Assert.That(failed.LivesAfter, Is.EqualTo(0));
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Showing);

            fail = manager.Get(UiName.Fail);
            revive = FindButton(
                fail,
                "ReviveButton",
                requireInteractable: false);
            yield return WaitUntil(
                () => revive.interactable,
                TransitionTimeoutSeconds,
                "Reused Fail ReviveButton did not unlock.");
            revive.onClick.Invoke();
            Assert.That(provider.ShowCount, Is.EqualTo(2));
            provider.EmitShown();
            provider.EmitClosed();
            yield return null;

            AssertShowing(manager, UiName.Fail);
            Assert.That(gameplay.SessionState,
                Is.EqualTo(GameSessionState.Failed),
                "Closing without ad_rewarded must not revive the session.");
            Assert.That(gameplay.LivesForTests, Is.EqualTo(0));
            Assert.That(revive.interactable, Is.True,
                "A failed reward attempt must re-enable ReviveButton.");
            Assert.That(
                GameStateRuntime.Current.GetGameTotalStat(
                    "main",
                    "revive_count"),
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AppScene_BankSpecialButtons_LaunchAndReturn()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow bank = manager.Show(UiName.Bank);
            Assert.That(bank, Is.Not.Null);
            AssertShowing(manager, UiName.Bank);

            BankRootCardView specialCard = FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard");
            FindOnlyButton(specialCard).onClick.Invoke();
            yield return null;

            BankLevelRowView firstSpecial =
                FindNamedComponent<BankLevelRowView>(bank, "SpecialRow1");
            Assert.That(firstSpecial.gameObject.activeInHierarchy, Is.True,
                "SP root card did not open the special-level list.");
            FindOnlyButton(firstSpecial).onClick.Invoke();

            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.SessionMode, Is.EqualTo(GameplaySessionMode.Bank));
            Assert.That(gameplay.CurrentPuzzleSize, Is.GreaterThan(0));

            Button returnBank = FindButton(game, "ReturnBankBtn");
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.True,
                "A bank session must expose ReturnBankBtn.");
            returnBank.onClick.Invoke();
            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Hidden);
            yield return WaitForState(manager, UiName.Bank,
                UiWindowState.Showing);

            Assert.That(specialCard.gameObject.activeInHierarchy, Is.True,
                "Returning from Game must restore Bank at its root panel.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_BankSpecialWinNextAndFailRestart_PreserveSession()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            UIFrameWindow bank = manager.Show(UiName.Bank);
            BankRootCardView specialCard = FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard");
            FindOnlyButton(specialCard).onClick.Invoke();
            yield return null;
            BankLevelRowView firstSpecial =
                FindNamedComponent<BankLevelRowView>(bank, "SpecialRow1");
            FindOnlyButton(firstSpecial).onClick.Invoke();

            yield return WaitForState(manager, UiName.Game,
                UiWindowState.Showing);
            UIFrameWindow game = manager.Get(UiName.Game);
            GameplayManager gameplay =
                game.GetComponentInChildren<GameplayManager>(true);
            Assert.That(gameplay, Is.Not.Null);
            yield return WaitForSession(gameplay, GameSessionState.Playing);
            Assert.That(gameplay.BankIndexForTests, Is.EqualTo(1));

            SessionActionResult completed = gameplay.RunAutoComplete();
            Assert.That(completed.Accepted, Is.True);
            Assert.That(completed.IsComplete, Is.True);
            yield return CompletePreWinMetaFlow(manager);

            UIFrameWindow win = manager.Get(UiName.Win);
            Button next = FindActiveButton(win, "Next", false);
            yield return WaitUntil(
                () => next.interactable,
                TransitionTimeoutSeconds,
                "Bank Win Next button did not become interactable.");
            next.onClick.Invoke();

            yield return WaitForState(
                manager,
                UiName.Win,
                UiWindowState.Hidden,
                15f);
            yield return WaitUntil(
                () => gameplay.SessionMode == GameplaySessionMode.Bank &&
                      gameplay.BankIndexForTests == 2 &&
                      gameplay.SessionState == GameSessionState.Playing,
                15f,
                "Bank Next did not load SP entry #2.");

            Button returnBank = FindButton(
                game,
                "ReturnBankBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.False,
                "Bank Next must drop the direct-browser return control like the source.");

            yield return FailCurrentSession(gameplay, manager);
            UIFrameWindow fail = manager.Get(UiName.Fail);
            Button restart = FindButton(fail, "RestartButton", false);
            yield return WaitUntil(
                () => restart.isActiveAndEnabled && restart.interactable,
                TransitionTimeoutSeconds,
                "Bank Fail RestartButton did not unlock.");
            restart.onClick.Invoke();
            yield return WaitForState(manager, UiName.Fail,
                UiWindowState.Hidden);
            yield return WaitForSession(gameplay, GameSessionState.Playing);

            Assert.That(gameplay.SessionMode, Is.EqualTo(GameplaySessionMode.Bank));
            Assert.That(gameplay.BankIndexForTests, Is.EqualTo(2));
            Assert.That(gameplay.LivesForTests, Is.EqualTo(3));
            Assert.That(returnBank.gameObject.activeInHierarchy, Is.False,
                "Restarting a post-Next bank entry must not restore direct-return UI.");
            AssertShowing(manager, UiName.Game);
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        [UnityTest]
        public IEnumerator AppScene_BankPoolMatrix_LaunchNextAndReuseDynamicRows()
        {
            AppBootstrap bootstrap = Find<AppBootstrap>();
            UIManager manager = Find<UIManager>();
            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(manager, Is.Not.Null);
            yield return WaitUntil(
                () => bootstrap.IsComplete ||
                      bootstrap.Phase == AppStartupPhase.Failed,
                StartupTimeoutSeconds,
                "AppBootstrap did not finish within the timeout.");
            Assert.That(bootstrap.Phase, Is.EqualTo(AppStartupPhase.Complete),
                bootstrap.FailureReason);

            var branches = new[]
            {
                ("RegularCard", BankPoolKind.Regular,
                    BankUiFlow.SizeThenTier),
                ("LKCard", BankPoolKind.Lk,
                    BankUiFlow.LevelRows),
                ("LKModifiedCard", BankPoolKind.LkModified,
                    BankUiFlow.LevelRows),
                ("LKStyleCard", BankPoolKind.LkStyle,
                    BankUiFlow.SizeThenTier),
                ("GCCard", BankPoolKind.Gc,
                    BankUiFlow.SizeThenTier)
            };

            UIFrameWindow bank = manager.Show(UiName.Bank);
            Assert.That(bank, Is.Not.Null);
            BankBrowserPagePresenter browser =
                bank.GetComponent<BankBrowserPagePresenter>();
            Assert.That(browser, Is.Not.Null);
            UIFrameWindow game = null;
            GameplayManager gameplay = null;

            foreach ((string rootName, BankPoolKind pool, BankUiFlow flow)
                     in branches)
            {
                BankRootCardView root = FindNamedComponent<BankRootCardView>(
                    bank,
                    rootName);
                Assert.That(root.gameObject.activeInHierarchy, Is.True,
                    rootName + " is unavailable with the shipped bank data.");
                FindOnlyButton(root).onClick.Invoke();
                yield return null;

                int expectedLaunchIndex = 1;

                if (flow == BankUiFlow.SizeThenTier)
                {
                    BankSizeCardView size =
                        FindFirstActiveComponent<BankSizeCardView>(bank);
                    FindOnlyButton(size).onClick.Invoke();
                    yield return null;
                    BankTierCardView tier =
                        FindFirstActiveComponent<BankTierCardView>(bank);
                    Assert.That(tier.CountForTests, Is.GreaterThan(0));
                    Assert.That(tier.NumberForTests, Is.EqualTo(1));
                    Button minus = FindChildButton(
                        tier,
                        "MinusBtn",
                        requireInteractable: false);
                    Assert.That(minus.interactable, Is.False,
                        rootName + " tier selector must clamp at one.");
                    minus.onClick.Invoke();
                    Assert.That(tier.NumberForTests, Is.EqualTo(1));
                    Button plus = FindChildButton(
                        tier,
                        "PlusBtn",
                        requireInteractable: false);
                    if (tier.CountForTests > 1)
                    {
                        Assert.That(plus.interactable, Is.True);
                        plus.onClick.Invoke();
                        Assert.That(tier.NumberForTests, Is.EqualTo(2));
                        Assert.That(minus.interactable, Is.True);
                        minus.onClick.Invoke();
                        Assert.That(tier.NumberForTests, Is.EqualTo(1));
                        Assert.That(minus.interactable, Is.False);
                        if (pool == BankPoolKind.Regular)
                        {
                            for (int number = 1;
                                 number < tier.CountForTests;
                                 number++)
                                plus.onClick.Invoke();
                            plus.onClick.Invoke();
                            Assert.That(tier.NumberForTests,
                                Is.EqualTo(tier.CountForTests));
                            Assert.That(plus.interactable, Is.False,
                                "Tier selector must clamp at its source count.");
                            for (int number = tier.CountForTests;
                                 number > 1;
                                 number--)
                                minus.onClick.Invoke();
                            minus.onClick.Invoke();
                            Assert.That(tier.NumberForTests, Is.EqualTo(1));
                        }
                        plus.onClick.Invoke();
                        expectedLaunchIndex = 2;
                    }
                    FindChildButton(tier, "GoBtn").onClick.Invoke();
                }
                else
                {
                    Assert.That(browser.StateForTests.Panel,
                        Is.EqualTo(BankBrowserPanel.LkList));
                    Assert.That(browser.LkCountForTests, Is.GreaterThan(0));
                    Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                    Button minus = FindActiveButton(
                        bank,
                        "MinusBtn",
                        requireInteractable: false);
                    Assert.That(minus.interactable, Is.False,
                        rootName + " selector must clamp at one.");
                    minus.onClick.Invoke();
                    Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                    Button plus = FindActiveButton(
                        bank,
                        "PlusBtn",
                        requireInteractable: false);
                    if (browser.LkCountForTests > 1)
                    {
                        Assert.That(plus.interactable, Is.True);
                        plus.onClick.Invoke();
                        Assert.That(browser.LkNumberForTests, Is.EqualTo(2));
                        Assert.That(minus.interactable, Is.True);
                        minus.onClick.Invoke();
                        Assert.That(browser.LkNumberForTests, Is.EqualTo(1));
                        Assert.That(minus.interactable, Is.False);
                        if (pool == BankPoolKind.Lk)
                        {
                            for (int number = 1;
                                 number < browser.LkCountForTests;
                                 number++)
                                plus.onClick.Invoke();
                            plus.onClick.Invoke();
                            Assert.That(browser.LkNumberForTests,
                                Is.EqualTo(browser.LkCountForTests));
                            Assert.That(plus.interactable, Is.False,
                                "LK selector must clamp at its source count.");
                            for (int number = browser.LkCountForTests;
                                 number > 1;
                                 number--)
                                minus.onClick.Invoke();
                            minus.onClick.Invoke();
                            Assert.That(browser.LkNumberForTests,
                                Is.EqualTo(1));
                        }
                        plus.onClick.Invoke();
                        expectedLaunchIndex = 2;
                    }
                    FindActiveButton(bank, "GoBtn").onClick.Invoke();
                }

                yield return WaitForState(manager, UiName.Game,
                    UiWindowState.Showing);
                game = manager.Get(UiName.Game);
                gameplay = game.GetComponentInChildren<GameplayManager>(true);
                Assert.That(gameplay, Is.Not.Null);
                yield return WaitForSession(gameplay,
                    GameSessionState.Playing);
                Assert.That(gameplay.SessionMode,
                    Is.EqualTo(GameplaySessionMode.Bank));
                Assert.That(gameplay.BankPoolForTests, Is.EqualTo(pool));
                Assert.That(gameplay.BankIndexForTests,
                    Is.EqualTo(expectedLaunchIndex));
                Assert.That(gameplay.BankTotalForTests, Is.GreaterThan(0));

                Button directReturn = FindButton(
                    game,
                    "ReturnBankBtn",
                    requireInteractable: false,
                    requireActive: false);
                Assert.That(directReturn.gameObject.activeInHierarchy, Is.True,
                    rootName + " launch must expose direct Bank return.");

                int expectedNext =
                    gameplay.BankIndexForTests % gameplay.BankTotalForTests + 1;
                SessionActionResult completed = gameplay.RunAutoComplete();
                Assert.That(completed.Accepted, Is.True);
                Assert.That(completed.IsComplete, Is.True);
                yield return CompletePreWinMetaFlow(manager);

                UIFrameWindow win = manager.Get(UiName.Win);
                Button next = FindActiveButton(win, "Next", false);
                yield return WaitUntil(
                    () => next.interactable,
                    TransitionTimeoutSeconds,
                    rootName + " Win Next button did not unlock.");
                next.onClick.Invoke();
                yield return WaitForState(
                    manager,
                    UiName.Win,
                    UiWindowState.Hidden,
                    15f);
                yield return WaitUntil(
                    () => gameplay.SessionState == GameSessionState.Playing &&
                          gameplay.BankPoolForTests == pool &&
                          gameplay.BankIndexForTests == expectedNext,
                    15f,
                    rootName + " Next did not preserve its Bank pool/index.");
                Assert.That(directReturn.gameObject.activeInHierarchy, Is.False,
                    rootName + " Next must drop direct-browser return.");

                manager.Show(UiName.Bank);
                manager.Hide(UiName.Game);
                yield return WaitForState(manager, UiName.Game,
                    UiWindowState.Hidden);
                AssertShowing(manager, UiName.Bank);
            }

            int sizePoolCount =
                bank.GetComponentsInChildren<BankSizeCardView>(true).Length;
            int tierPoolCount =
                bank.GetComponentsInChildren<BankTierCardView>(true).Length;

            for (int cycle = 0; cycle < 8; cycle++)
            {
                BankRootCardView regular =
                    FindNamedComponent<BankRootCardView>(bank, "RegularCard");
                FindOnlyButton(regular).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.RegularSize));
                BankSizeCardView size =
                    FindFirstActiveComponent<BankSizeCardView>(bank);
                FindOnlyButton(size).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Tier));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.RegularSize));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Root));
            }

            BankRootCardView lkStyle =
                FindNamedComponent<BankRootCardView>(bank, "LKStyleCard");
            FindOnlyButton(lkStyle).onClick.Invoke();
            yield return null;
            FindOnlyButton(FindFirstActiveComponent<BankSizeCardView>(bank))
                .onClick.Invoke();
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Tier));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.VariantSize));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;

            BankRootCardView gc =
                FindNamedComponent<BankRootCardView>(bank, "GCCard");
            FindOnlyButton(gc).onClick.Invoke();
            yield return null;
            FindOnlyButton(FindFirstActiveComponent<BankSizeCardView>(bank))
                .onClick.Invoke();
            yield return null;
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Root),
                "GC Tier back follows the source and returns to Bank root.");

            foreach (string rootName in new[] { "LKCard", "LKModifiedCard" })
            {
                FindOnlyButton(FindNamedComponent<BankRootCardView>(
                    bank,
                    rootName)).onClick.Invoke();
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.LkList));
                Assert.That(manager.RequestBack(), Is.True);
                yield return null;
                Assert.That(browser.StateForTests.Panel,
                    Is.EqualTo(BankBrowserPanel.Root));
            }

            FindOnlyButton(FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard")).onClick.Invoke();
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.LevelList));
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;
            Assert.That(browser.StateForTests.Panel,
                Is.EqualTo(BankBrowserPanel.Root));

            int levelPoolCount =
                bank.GetComponentsInChildren<BankLevelRowView>(true).Length;
            FindOnlyButton(FindNamedComponent<BankRootCardView>(
                bank,
                "SPCard")).onClick.Invoke();
            yield return null;
            Assert.That(manager.RequestBack(), Is.True);
            yield return null;

            Assert.That(
                bank.GetComponentsInChildren<BankSizeCardView>(true).Length,
                Is.EqualTo(sizePoolCount),
                "Back-stack stress must reuse dynamic size rows.");
            Assert.That(
                bank.GetComponentsInChildren<BankTierCardView>(true).Length,
                Is.EqualTo(tierPoolCount),
                "Back-stack stress must reuse dynamic tier rows.");
            Assert.That(
                bank.GetComponentsInChildren<BankLevelRowView>(true).Length,
                Is.EqualTo(levelPoolCount),
                "Back-stack stress must reuse dynamic level rows.");
            Assert.That(manager.IsAnyLoading, Is.False);
        }

        private static IEnumerator CompletePreWinMetaFlow(
            UIManager manager,
            UiName resultRoute = UiName.Win)
        {
            float deadline = Time.realtimeSinceStartup + 35f;
            while (!IsShowing(manager, resultRoute) &&
                   Time.realtimeSinceStartup < deadline)
            {
                if (IsShowing(manager, UiName.Award))
                {
                    yield return CollectAward(manager);
                    continue;
                }

                if (IsShowing(manager, UiName.Streak))
                {
                    UIFrameWindow streak = manager.Get(UiName.Streak);
                    Button lit = FindButton(
                        streak,
                        "LitTapSurface",
                        requireInteractable: false,
                        requireActive: false);
                    if (lit.gameObject.activeInHierarchy)
                    {
                        yield return WaitUntil(
                            () => lit.interactable,
                            TransitionTimeoutSeconds,
                            "Streak Lit input did not unlock.");
                        lit.onClick.Invoke();
                    }

                    Button claim = FindButton(
                        streak,
                        "ClaimBtn",
                        requireInteractable: false,
                        requireActive: false);
                    while ((!claim.gameObject.activeInHierarchy ||
                            !claim.interactable) &&
                           Time.realtimeSinceStartup < deadline)
                    {
                        if (IsShowing(manager, UiName.Award))
                            yield return CollectAward(manager);
                        else
                            yield return null;
                    }
                    Assert.That(claim.gameObject.activeInHierarchy, Is.True,
                        "Streak ClaimBtn did not become active.");
                    Assert.That(claim.interactable, Is.True,
                        "Streak ClaimBtn did not unlock.");
                    claim.onClick.Invoke();
                    yield return WaitForState(
                        manager,
                        UiName.Streak,
                        UiWindowState.Hidden,
                        10f);
                    continue;
                }

                yield return null;
            }

            Assert.That(IsShowing(manager, resultRoute), Is.True,
                resultRoute +
                " did not appear after completing pre-result meta flow.");
        }

        private static IEnumerator CollectAward(UIManager manager)
        {
            UIFrameWindow award = manager.Get(UiName.Award);
            Button collect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => collect.interactable,
                5f,
                "Award CollectBtn did not unlock.");
            collect.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Award,
                UiWindowState.Hidden,
                10f);
        }

        private static IEnumerator CollectRankGift(UIManager manager)
        {
            UIFrameWindow award = manager.Get(UiName.Award);
            Button podiumCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            yield return WaitUntil(
                () => podiumCollect.interactable,
                5f,
                "Rank Gift podium CollectBtn did not unlock.");
            podiumCollect.onClick.Invoke();
            yield return null;
            Assert.That(IsShowing(manager, UiName.Award), Is.True,
                "Rank Gift with a chest must keep Award open for item phase.");

            yield return WaitUntil(
                () => !podiumCollect.gameObject.activeInHierarchy,
                5f,
                "Rank Gift chest did not reach its source item-phase cue.");

            Button itemCollect = FindActiveButton(
                award,
                "CollectBtn",
                requireInteractable: false);
            Assert.That(itemCollect, Is.Not.SameAs(podiumCollect));
            yield return WaitUntil(
                () => itemCollect.interactable,
                5f,
                "Rank Gift item CollectBtn did not unlock.");
            itemCollect.onClick.Invoke();
            yield return WaitForState(
                manager,
                UiName.Award,
                UiWindowState.Hidden,
                10f);
        }

        private static IEnumerator PresentPendingStreak(
            UIManager manager,
            StreakFeature streak,
            int checkedBeforeReveal,
            int checkedAfterReveal)
        {
            Assert.That(streak.HasPendingShow, Is.True);
            int pendingUid = streak.PendingShowUid;
            StreakDisplayState requested =
                streak.Data.CurrentStreak == 1 &&
                !streak.ShouldSkipLit
                    ? StreakDisplayState.Lit
                    : StreakDisplayState.Settle;
            UIFrameWindow frame = manager.Show(
                UiName.Streak,
                new Dictionary<string, object>
                {
                    [StreakPagePresenter.StateParameter] = (int)requested
                });
            Assert.That(frame, Is.Not.Null);
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Showing);
            StreakPagePresenter page =
                frame.GetComponentInChildren<StreakPagePresenter>(true);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.StateForTests, Is.EqualTo(requested));

            if (requested == StreakDisplayState.Lit)
            {
                Button lit = FindButton(
                    frame,
                    "LitTapSurface",
                    requireInteractable: false,
                    requireActive: false);
                yield return WaitUntil(
                    () => lit.interactable,
                    TransitionTimeoutSeconds,
                    "Streak Lit input did not unlock.");
                lit.onClick.Invoke();
                Assert.That(page.StateForTests,
                    Is.EqualTo(StreakDisplayState.Settle));
            }

            Assert.That(CountCheckedStreakSlots(page),
                Is.EqualTo(checkedBeforeReveal),
                "Settle must initially hide the newest check-in like Godot.");
            Button claim = FindButton(
                frame,
                "ClaimBtn",
                requireInteractable: false,
                requireActive: false);
            Assert.That(claim.interactable, Is.False);

            if (pendingUid > 0)
            {
                yield return WaitForState(manager, UiName.Award,
                    UiWindowState.Showing,
                    8f);
                yield return CollectAward(manager);
            }

            yield return WaitUntil(
                () => claim.interactable,
                5f,
                "Streak Continue did not unlock after settle.");
            Assert.That(page.SettleRevealCompleteForTests, Is.True);
            Assert.That(CountCheckedStreakSlots(page),
                Is.EqualTo(checkedAfterReveal),
                "Settle did not reveal the new check-in slot.");
            claim.onClick.Invoke();
            yield return WaitForState(manager, UiName.Streak,
                UiWindowState.Hidden,
                10f);
        }

        private static int CountCheckedWeekSlots(StreakFeature streak)
        {
            int count = 0;
            IReadOnlyList<StreakWeekSlot> slots = streak.GetWeekSlots();
            for (int index = 0; index < slots.Count; index++)
                if (slots[index].IsChecked)
                    count++;
            return count;
        }

        private static int CountCheckedStreakSlots(StreakPagePresenter page)
        {
            int count = 0;
            StreakDaySlotView[] slots =
                page.GetComponentsInChildren<StreakDaySlotView>(true);
            Assert.That(slots.Length, Is.EqualTo(StreakFeature.CycleLength));
            for (int index = 0; index < slots.Length; index++)
                if (slots[index].IsCheckedForTests)
                    count++;
            return count;
        }

        private static IEnumerator ShowThenHide(
            UIManager manager,
            UiName route)
        {
            UIFrameWindow page = manager.Show(route);
            Assert.That(page, Is.Not.Null, route + " is not registered.");
            Assert.That(page.WindowState, Is.EqualTo(UiWindowState.Showing));
            manager.Hide(route);
            yield return WaitForState(manager, route, UiWindowState.Hidden);
        }

        private static IEnumerator WaitForState(
            UIManager manager,
            UiName route,
            UiWindowState expected,
            float timeoutSeconds = TransitionTimeoutSeconds)
        {
            yield return WaitUntil(
                () => manager.Get(route)?.WindowState == expected,
                timeoutSeconds,
                route + " did not reach state " + expected + ".");
        }

        private static IEnumerator WaitForSession(
            GameplayManager gameplay,
            GameSessionState expected)
        {
            yield return WaitUntil(
                () => gameplay.SessionState == expected,
                TransitionTimeoutSeconds,
                "Gameplay did not reach session state " + expected + ".");
        }

        private static IEnumerator FailCurrentSession(
            GameplayManager gameplay,
            UIManager manager,
            UiName failRoute = UiName.Fail,
            UiName gameRoute = UiName.Game)
        {
            for (int mistake = 0; mistake < 3; mistake++)
            {
                yield return WaitForSession(
                    gameplay,
                    GameSessionState.Playing);
                Vector2Int cell = FindEmptyWrongCell(gameplay);
                SessionActionResult result = gameplay.DoubleTapForTests(
                    cell.x,
                    cell.y);
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.Kind,
                    Is.EqualTo(SessionActionKind.WrongGuess));
                Assert.That(result.LivesAfter, Is.EqualTo(2 - mistake));
            }
            yield return WaitForSession(gameplay, GameSessionState.Failed);
            yield return WaitForState(manager, failRoute,
                UiWindowState.Showing);

            UIFrameWindow failPage = manager.Get(failRoute);
            UIFrameWindow gamePage = manager.Get(gameRoute);
            Assert.That(failPage, Is.Not.Null);
            Assert.That(gamePage, Is.Not.Null);
            Assert.That(manager.IsInputBrieflyBlocked(
                    failPage.transform as RectTransform),
                Is.True,
                "Fail must block its whole page for the source 1.5 seconds.");
            Assert.That(manager.IsInputBrieflyBlocked(
                    gamePage.transform as RectTransform),
                Is.True,
                "The terminal wrong guess must block Game for 2 seconds.");
            yield return WaitUntil(
                () => !manager.IsInputBrieflyBlocked(
                    failPage.transform as RectTransform),
                3f,
                "Fail page input blocker did not release.");
        }

        private static void AssertDailyDidNotMutateMainState(
            GameStateService state)
        {
            Assert.That(state.CurrentLevel, Is.EqualTo(21));
            Assert.That(state.CurrentStrategy, Is.EqualTo(3));
            Assert.That(state.Data.ConsecutiveFails, Is.EqualTo(5));
            Assert.That(state.Data.RetryPuzzleLevel, Is.EqualTo(21));
            Assert.That(state.Data.RetryPuzzleParameters["sentinel"],
                Is.EqualTo("main-retry"));
            Assert.That(state.GetEndgameSnapshot()["sentinel"],
                Is.EqualTo("main-snapshot"));
            Assert.That(state.Data.MainGameTotalStats["sentinel"],
                Is.EqualTo(7));
            Assert.That(state.IsCurrentLevelDirty, Is.False);
            Assert.That(state.WasDdaToolOrReviveUsed, Is.False);
            Assert.That(state.WasDdaReviveUsed, Is.False);
        }

        private static Vector2Int FindEmptyWrongCell(
            GameplayManager gameplay)
        {
            int size = gameplay.CurrentPuzzleSize;
            for (int row = 0; row < size; row++)
            {
                int solution = gameplay.SolutionColumnForTests(row);
                for (int column = 0; column < size; column++)
                {
                    if (column == solution ||
                        gameplay.GetCellState(row, column) !=
                        CellStateType.EMPTY)
                        continue;
                    return new Vector2Int(row, column);
                }
            }
            Assert.Fail("No empty wrong cell remained for the fail fixture.");
            return new Vector2Int(-1, -1);
        }

        private static IEnumerator WaitUntil(
            Func<bool> predicate,
            float timeoutSeconds,
            string failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!predicate() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(predicate(), Is.True, failureMessage);
        }

        private static PrivacyPermissionRuntime CreatePlatformRuntime(
            UIManager manager,
            AbConfigRuntime abRuntime,
            out PlayModePlatformPermissionProvider provider)
        {
            var host = new GameObject("PlatformPermissionRuntimeTest");
            provider = host.AddComponent<
                PlayModePlatformPermissionProvider>();
            PrivacyPermissionRuntime runtime =
                host.AddComponent<PrivacyPermissionRuntime>();
            runtime.ConfigureForTests(
                manager,
                abRuntime,
                tracking: Find<TrackingRuntime>());
            runtime.BindProvider(provider);
            return runtime;
        }

        private static IEnumerator CompleteWhenDone(
            IEnumerator routine,
            Action completed)
        {
            yield return routine;
            completed?.Invoke();
        }

        private static void ClickThroughPointerPhases(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(EventSystem.current, Is.Not.Null,
                "AppScene is missing an active EventSystem.");
            var eventData = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1,
                clickCount = 1,
                position = RectTransformUtility.WorldToScreenPoint(
                    null,
                    button.transform.position)
            };
            Assert.That(ExecuteEvents.Execute(
                    button.gameObject,
                    eventData,
                    ExecuteEvents.pointerDownHandler),
                Is.True);
            Assert.That(ExecuteEvents.Execute(
                    button.gameObject,
                    eventData,
                    ExecuteEvents.pointerUpHandler),
                Is.True);
            // ExecuteEvents does not build EventSystem's pointer-click state
            // machine when called in isolation. Invoke the Button event after
            // the real down/up handlers so the ordering matches UGUI: release
            // is queued first, then the navigation callback opens the page.
            button.onClick.Invoke();
        }

        private static void AssertLocalInputBlocker(RectTransform target)
        {
            Assert.That(target, Is.Not.Null);
            Transform blocker = target.Find("_InputBlocker");
            Assert.That(blocker, Is.Not.Null,
                "Target has no local _InputBlocker child.");
            Image image = blocker.GetComponent<Image>();
            Canvas canvas = blocker.GetComponent<Canvas>();
            Assert.That(image, Is.Not.Null);
            Assert.That(image.raycastTarget, Is.True);
            Assert.That(blocker.GetComponent<GraphicRaycaster>(), Is.Not.Null);
            Assert.That(canvas, Is.Not.Null);
            Assert.That(canvas.overrideSorting, Is.True);
            Assert.That(canvas.sortingOrder, Is.EqualTo(4095));

            int count = 0;
            foreach (Transform child in target)
                if (child.name == "_InputBlocker") count++;
            Assert.That(count, Is.EqualTo(1),
                "Refreshing a target must leave exactly one local blocker.");
        }

        private static void AssertShowing(UIManager manager, UiName route)
        {
            UIFrameWindow page = manager.Get(route);
            Assert.That(page, Is.Not.Null, route + " has not been created.");
            Assert.That(page.WindowState, Is.EqualTo(UiWindowState.Showing));
        }

        private static bool IsShowing(UIManager manager, UiName route)
        {
            return manager.Get(route)?.WindowState == UiWindowState.Showing;
        }

        private static Button FindButton(
            UIFrameWindow page,
            string name,
            bool requireInteractable = true,
            bool requireActive = true)
        {
            Assert.That(page, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = page.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != name) continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected exactly one " + name + ".");
            if (requireActive)
                Assert.That(found.isActiveAndEnabled, Is.True,
                    page.UiName + "/" + name + " is not active.");
            if (requireInteractable)
                Assert.That(found.interactable, Is.True,
                    page.UiName + "/" + name + " is not interactable.");
            return found;
        }

        private static Button FindActiveButton(
            UIFrameWindow page,
            string name,
            bool requireInteractable = true)
        {
            Assert.That(page, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = page.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != name || !button.gameObject.activeInHierarchy)
                    continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected one active " + name + ".");
            if (requireInteractable)
                Assert.That(found.interactable, Is.True,
                    page.UiName + "/" + name + " is not interactable.");
            return found;
        }

        private static Button FindEntryButton(Component entry)
        {
            Assert.That(entry, Is.Not.Null);
            Button found = null;
            int count = 0;
            Button[] buttons = entry.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                if (button.name != "ClickBtn") continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                entry.name + " expected exactly one ClickBtn.");
            return found;
        }

        private static T FindNamedComponent<T>(
            UIFrameWindow page,
            string name) where T : Component
        {
            Assert.That(page, Is.Not.Null);
            T found = null;
            int count = 0;
            T[] components = page.GetComponentsInChildren<T>(true);
            foreach (T component in components)
            {
                if (component.name != name) continue;
                found = component;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                page.UiName + " expected exactly one " + name + ".");
            return found;
        }

        private static T FindFirstActiveComponent<T>(
            UIFrameWindow page) where T : Component
        {
            Assert.That(page, Is.Not.Null);
            T[] components = page.GetComponentsInChildren<T>(true);
            foreach (T component in components)
                if (component.gameObject.activeInHierarchy)
                    return component;
            Assert.Fail(page.UiName + " has no active " + typeof(T).Name + ".");
            return null;
        }

        private static Button FindChildButton(
            Component component,
            string name,
            bool requireInteractable = true)
        {
            Assert.That(component, Is.Not.Null);
            Button found = null;
            int count = 0;
            foreach (Button button in
                     component.GetComponentsInChildren<Button>(true))
            {
                if (button.name != name) continue;
                found = button;
                count++;
            }
            Assert.That(count, Is.EqualTo(1),
                component.name + " expected exactly one " + name + ".");
            Assert.That(found.isActiveAndEnabled, Is.True);
            if (requireInteractable)
                Assert.That(found.interactable, Is.True);
            return found;
        }

        private static Button FindOnlyButton(Component component)
        {
            Assert.That(component, Is.Not.Null);
            Button[] buttons = component.GetComponentsInChildren<Button>(true);
            Assert.That(buttons.Length, Is.EqualTo(1),
                component.name + " expected exactly one Button.");
            Assert.That(buttons[0].isActiveAndEnabled, Is.True,
                component.name + " button is not active.");
            Assert.That(buttons[0].interactable, Is.True,
                component.name + " button is not interactable.");
            return buttons[0];
        }

        private static T Find<T>() where T : Object
        {
            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }
    }
}

``

