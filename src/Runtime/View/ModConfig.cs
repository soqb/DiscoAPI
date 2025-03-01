using System;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using LocalizationCustomSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SV = Sunshine.Views;

namespace DiscoAPI.Runtime.View;

public class ModConfigView
{
	// don't worry about it!
	private static GameObject modsView = null!;
	private static GameObject modsButton = null!;
	private static TMPro.TextMeshProUGUI modsText = null!;
	private static Transform scrollView = null!;
	private static GameObject resetButton = null!;

	private static GameObject generalPrefab = null!;
	private static GameObject checkboxPrefab = null!;
	private static GameObject dropdownPrefab = null!;
	private static GameObject sliderPrefab = null!;
	private static ColorBlock colorBlock = new();

	public static void EnsureTabInstalled(Transform view)
	{
		if (view.Find("Content/Header/Tabs/ModsButton")) return;

		resetButton = view.Find("Content/ResetSettings").gameObject;
		var tabContainer = view.Find("Content/Header/Tabs");

		var tabs = new Transform[] { tabContainer.Find("SettingsButton"), tabContainer.Find("ControlsButton") };
		foreach (var tab in tabs)
		{
			tab.Find("Text").GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
			tab.GetComponent<RectTransform>().sizeDelta += new Vector2(-95f, 0f);
		}

		modsButton = GameObject.Instantiate(tabs[0].gameObject, tabContainer, true);
		modsButton.name = "ModsButton";
		var butt = modsButton.GetComponent<Button>();
		butt.onClick.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.Off);
		butt.onClick.AddListener((System.Action)(() => SetOptionsTab(SettingsHeaderController.singleton, OptionsTab.Mods)));

		{
			// there *might* be an extra background image if we're unlucky !!
			var bg = modsButton.transform.Find("Image");
			if (bg != null) GameObject.Destroy(bg.gameObject);
		}

		modsText = modsButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
		modsText.SetText("Mods", true);
		modsText.GetComponent<I2.Loc.Localize>().SetTerm($"\0RAW\0Mods");
		modsText.color = SettingsHeaderController.singleton.inactive;

		scrollView = view.Find("Content/ScrollMask/Scroll View");
		modsView = new GameObject("ModConfig");
		modsView.SetActive(false);
		modsView.transform.localPosition += new Vector3(0f, 0f, 100f);
		MainThreadExecutor.Queue(() =>
		{
			// tbqh idk and idc.
			modsView.transform.localScale = Vector3.one;
		});
		modsView.transform.parent = scrollView;
		{
			var settings = SettingsHeaderController.singleton.settingsView.GetComponent<RectTransform>();
			var rect = modsView.AddComponent<RectTransform>();
			rect.anchoredPosition = settings.anchoredPosition;
			rect.anchoredPosition3D = settings.anchoredPosition3D;
			rect.anchorMax = settings.anchorMax;
			rect.anchorMin = settings.anchorMin;
			rect.pivot = settings.pivot;
			rect.offsetMin = settings.offsetMin;
			rect.offsetMax = settings.offsetMax;
			rect.sizeDelta = settings.sizeDelta;

			var fitter = modsView.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			var layout = modsView.AddComponent<LayoutElement>();
			layout.ignoreLayout = true;

			var vert = modsView.AddComponent<VerticalLayoutGroup>();
			vert.childControlHeight = true;
			vert.childControlWidth = false;
			vert.childForceExpandHeight = false;
			vert.childForceExpandWidth = true;
			vert.spacing = 15f;
			vert.childAlignment = TextAnchor.UpperLeft;
			vert.padding = new(0, 4, 0, 0);
			vert.SetDirty();
		}

		generalPrefab = scrollView.Find("Settings/Controls Options/Controller Vibration Toggle").gameObject;
		checkboxPrefab = scrollView.Find("Settings/Audio Options/Dyslexic Font Toggle/Checkbox").gameObject;
		dropdownPrefab = scrollView.Find("Settings/Graphics Options/Display Mode/Dropdown").gameObject;
		sliderPrefab = scrollView.Find("Settings/Graphics Options/LayoutProfileSlider/Slider").gameObject;
		colorBlock = generalPrefab.GetComponent<Toggle>().colors;

		foreach (var src in DiscoRunner.manager.linearSources)
		{
			if (src.ConfigFile == null || src.ConfigFile.Values.Count == 0) continue;

			var cat = CreateCategory($"mod config category {src.Guid}", modsView.transform);

			var title = CreateOptionBase(
				$"mod config title {src.Guid}",
				new(HumanizeName(src.DisplayName ?? src.Guid), src.Description),
				cat.transform
			);
			var titleLabel = title.transform.Find("Label");
			titleLabel.GetComponent<TextMeshProUGUI>().color = colorBlock.highlightedColor;
			UnityEngine.Object.Destroy(titleLabel.GetComponent<OptionLabelHighlightController>());
			UnityEngine.Object.Destroy(title.GetComponent<OptionSelectableController>());
			UnityEngine.Object.Destroy(title.GetComponent<Image>());
			UnityEngine.Object.Destroy(title.GetComponent<Coffee.UISoftMask.SoftMaskable>());

			foreach (var stg in src.ConfigFile.Values)
			{
				var installer = InstallerForSetting(stg.SettingType, stg.Description, stg.BoxedValue, (v) => stg.BoxedValue = v);
				if (installer == null) continue;

				var setting = CreateOptionBase(
					$"mod config option {src.Guid}/{stg.Definition.Section}/{stg.Definition.Key}",
					new(HumanizeName(stg.Definition.Key), stg.Description.Description),
					cat.transform
				);
				installer(setting);
			}

			if (src != DiscoRunner.manager.linearSources[DiscoRunner.manager.linearSources.Count - 1])
			{
				CreateSeparator(modsView.transform);
			}
		}
	}

	private static readonly Regex RgWordStart = new Regex(@"(\B[A-Z]+(?=[A-Z0-9]|\b))|(\B[0-9]+)|(\B[A-Z0-9])", RegexOptions.Compiled);

	private static string HumanizeName(string name) => RgWordStart.Replace(name, " $0");

	private static GameObject CreateCategory(string name, Transform parent)
	{
		var cat = new GameObject(name);
		cat.transform.parent = parent;

		var rect = cat.AddComponent<RectTransform>();
		rect.sizeDelta = new(811f, 0f);

		var vert = cat.AddComponent<VerticalLayoutGroup>();
		vert.childControlHeight = false;
		vert.childControlWidth = true;
		vert.childForceExpandHeight = false;
		vert.childForceExpandWidth = true;
		vert.spacing = 1f;
		vert.childAlignment = TextAnchor.UpperCenter;
		vert.padding = new(12, 0, 0, 0);
		vert.SetDirty();

		return cat;
	}

	private static GameObject CreateSeparator(Transform parent)
	{
		var cat = new GameObject("Separator");
		cat.transform.parent = parent;

		var rect = cat.AddComponent<RectTransform>();
		return cat;
	}

	private static GameObject InstallCheckbox(GameObject parent, bool now, Action<bool> onChange)
	{
		var box = GameObject.Instantiate(checkboxPrefab, parent.transform, false);
		UnityEngine.Object.Destroy(box.GetComponent<Sunshine.TooltipSource>());
		UnityEngine.Object.Destroy(box.GetComponent<LocalizedTooltipDescription>());

		var toggle = parent.AddComponent<Toggle>();
		toggle.isOn = now;
		toggle.graphic = box.transform.Find("Background/Checkmark").GetComponent<Image>();
		toggle.targetGraphic = parent.transform.Find("Label").GetComponent<TextMeshProUGUI>();
		toggle.colors = colorBlock;
		toggle.onValueChanged.AddListener(onChange);

		return box;
	}

	private delegate void OptionInstaller(GameObject setting);
	private static OptionInstaller? InstallerForSetting(Type ty, ConfigDescription desc, object value, Action<object> setValue)
	{
		if (ty == typeof(bool))
		{
			return (setting) => InstallCheckbox(setting, (bool)value, (v) => setValue(v));
		}

		return null;
	}

	public record struct SettingInfo(string title, string? description);

	private static GameObject CreateOptionBase(string settingName, SettingInfo info, Transform cat)
	{
		var toggle = GameObject.Instantiate(generalPrefab, cat, false);
		toggle.name = settingName;
		UnityEngine.Object.DestroyImmediate(toggle.transform.Find("Checkbox").gameObject);
		UnityEngine.Object.DestroyImmediate(toggle.GetComponent<Toggle>());
		UnityEngine.Object.Destroy(toggle.GetComponent<VibrationToggleConfiguration>());

		var label = toggle.transform.Find("Label");
		label.GetComponent<VariableTerm>().baseTerm = $"\0RAW\0{info.title}";
		label.GetComponent<PlatformSpecificLocalization>().PCTerm = new TranslationString() { Term = $"\0RAW\0{info.title}" };
		label.GetComponent<I2.Loc.Localize>().SetTerm($"\0RAW\0{info.title}");
		label.GetComponent<TextMeshProUGUI>().SetText(info.title);

		var tooltip = label.Find("Tooltip").gameObject;
		UnityEngine.Object.Destroy(tooltip.GetComponent<LocalizedPlatformSpecificTooltipDescription>());

		var loc = tooltip.AddComponent<LocalizedTooltipDescription>();
		loc.title = new TranslationString() { Term = $"\0RAW\0{info.title}" };
		loc.Description = new TranslationString() { Term = $"\0RAW\0{info.description ?? info.title}" };

		return toggle;
	}

	// public static GameObject CreateOption(string name, Transform category)
	// {
	// 	var opt = new GameObject(name);
	// 	opt.transform.parent = category;
	// 	opt.AddComponent<RectTransform>();

	// 	var bg = new GameObject("background");
	// 	bg.transform.parent = opt.transform;
	// 	bg.AddComponent<RectTransform>();
	// 	bg.AddComponent<CanvasRenderer>();
	// 	var bgim = bg.AddComponent<Image>();
	// 	bgim.activeSprite =

	// 	var choose = new GameObject("Choose");
	// 	choose.transform.parent = opt.transform;
	// 	var label = new GameObject("Label");
	// 	label.transform.parent = opt.transform;

	// 	return opt;
	// }

	public enum OptionsTab
	{
		Settings,
		Controls,
		Mods,
	}

	public static void SetOptionsTab(SettingsHeaderController header, OptionsTab tab)
	{
		header.settingsView.SetActive(tab == OptionsTab.Settings);
		header.controlsView.SetActive(tab == OptionsTab.Controls);
		modsView.SetActive(tab == OptionsTab.Mods);
		GameObject button = tab switch
		{
			OptionsTab.Settings => header.settingsButton,
			OptionsTab.Controls => header.controlsButton,
			OptionsTab.Mods => modsButton,
			_ => throw new Exception("expected a valid OptionsTab"),
		};
		header.settingsTabHighlight.rectTransform.SetParent(button.transform, false);
		header.settingsTabHighlight.rectTransform.SetSiblingIndex(0);
		header.settingsText.color = tab == OptionsTab.Settings ? header.active : header.inactive;
		header.controlsText.color = tab == OptionsTab.Controls ? header.active : header.inactive;
		modsText.color = tab == OptionsTab.Mods ? header.active : header.inactive;
		SV.OptionsScreen.Singleton.SelectSavedOption();
		SV.OptionsScreen.Singleton.isOnControllerView = tab != OptionsTab.Settings;

		resetButton.gameObject.SetActive(tab == OptionsTab.Settings);

		GameObject? view = tab switch
		{
			OptionsTab.Settings => header.settingsView,
			OptionsTab.Mods => modsView,
			_ => null,
		};

		if (view != null)
		{
			RectTransform t = view.GetComponent<RectTransform>();
			scrollView.GetComponent<VerticalStepScrollView>().content = t;
			scrollView.GetComponent<ScrollRect>().content = t;
		}
	}
}
