using System;
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

	public static void EnsureTabInstalled(Transform view)
	{
		if (view.Find("Content/Header/Tabs/ModsButton")) return;

		var tabContainer = view.Find("Content/Header/Tabs");

		var tabs = new Transform[] { tabContainer.Find("SettingsButton"), tabContainer.Find("ControlsButton") };
		foreach (var tab in tabs)
		{
			tab.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().alignment = TMPro.TextAlignmentOptions.Center;
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

		modsText = modsButton.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>();
		modsText.SetText("Mods", true);
		modsText.color = SettingsHeaderController.singleton.inactive;

		scrollView = view.Find("Content/ScrollMask/Scroll View");
		modsView = new GameObject("ModConfig");
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

		var cat = CreateCategory("DummyModConfig", modsView.transform);
		// GameObject.Instantiate(scrollView.Find("Settings/Audio Options/Music Volume"), cat.transform, false);
		// var mask = view.transform.Find("Content/ScrollMask");
		// var mymask = GameObject.Instantiate(mask.gameObject, modsContainer.transform, true);
	}

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
