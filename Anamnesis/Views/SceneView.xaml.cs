﻿// © Anamnesis.
// Licensed under the MIT license.

namespace Anamnesis.Views;

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Anamnesis.Files;
using Anamnesis.Memory;
using Anamnesis.Services;
using Anamnesis.Styles.Drawers;
using PropertyChanged;
using Serilog;

/// <summary>
/// Interaction logic for SceneView.xaml.
/// </summary>
[AddINotifyPropertyChangedInterface]
public partial class SceneView : UserControl
{
	private static DirectoryInfo? lastLoadDir;
	private static DirectoryInfo? lastSaveDir;

	public SceneView()
	{
		this.InitializeComponent();

		this.ContentArea.DataContext = this;
	}

	public GameService GameService => GameService.Instance;
	public TargetService TargetService => TargetService.Instance;
	public GposeService GposeService => GposeService.Instance;
	public TerritoryService TerritoryService => TerritoryService.Instance;
	public TimeService TimeService => TimeService.Instance;
	public CameraService CameraService => CameraService.Instance;
	public TipService TipService => TipService.Instance;
	public SettingsService SettingsService => SettingsService.Instance;

	private static ILogger Log => Serilog.Log.ForContext<SceneView>();

	private void OnTipClicked(object sender, RoutedEventArgs e)
	{
		TipService.Instance.KnowMore();
	}

	private void OnWeatherClicked(object sender, RoutedEventArgs e)
	{
		WeatherSelector selector = new WeatherSelector();
		SelectorDrawer.Show(selector, this.TerritoryService.CurrentWeather, (w) =>
		{
			this.TerritoryService.CurrentWeather = w;
		});
	}

	private async void OnLoadCamera(object sender, RoutedEventArgs e)
	{
		ActorBasicMemory? targetActor = this.TargetService.PlayerTarget;
		if (targetActor == null || !targetActor.IsValid)
			return;
		ActorMemory actorMemory = new ActorMemory();
		actorMemory.SetAddress(targetActor.Address);

		try
		{
			Shortcut[]? shortcuts = new[]
			{
					FileService.DefaultCameraDirectory,
			};

			Type[] types = new[]
			{
						typeof(CameraShotFile),
			};

			OpenResult result = await FileService.Open(lastLoadDir, shortcuts, types);

			if (result.File == null)
				return;

			lastLoadDir = result.Directory;

			if (result.File is CameraShotFile camFile)
			{
				camFile.Apply(CameraService.Instance, actorMemory);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to load camera");
		}
	}

	private async void OnSaveCamera(object sender, RoutedEventArgs e)
	{
		ActorBasicMemory? targetActor = this.TargetService.PlayerTarget;
		if (targetActor == null || !targetActor.IsValid)
			return;
		ActorMemory actorMemory = new ActorMemory();
		actorMemory.SetAddress(targetActor.Address);

		SaveResult result = await FileService.Save<CameraShotFile>(lastSaveDir, FileService.DefaultCameraDirectory);

		if (result.Path == null)
			return;

		lastSaveDir = result.Directory;

		CameraShotFile file = new CameraShotFile();
		file.WriteToFile(CameraService.Instance, actorMemory);

		using FileStream stream = new FileStream(result.Path.FullName, FileMode.Create);
		file.Serialize(stream);
	}
}
