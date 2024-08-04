﻿using System;
using System.Windows;

namespace HelloSwitcher.Views;

public partial class MenuWindow : Window
{
	public MenuWindow()
	{
		InitializeComponent();

		this.ContextMenu.Closed += OnContextMenuClosed;
	}

	public void ShowContextMenu()
	{
		// Show ContextMenu and then show Window itself.
		this.ContextMenu.IsOpen = true;
		base.Show();
	}

	protected override void OnActivated(EventArgs e)
	{
		base.OnActivated(e);

		// Get ContextMenu's location and place Window exactly behind it.
		// This is not essential but Window needs to be placed somewhere on the desktop and
		// ContextMenu's location seems to be most natural.
		if (WindowHelper.TryGetScreenLocation(this.ContextMenu, out Rect location))
			WindowHelper.SetWindowLocation(this, location);
	}

	protected override void OnDeactivated(EventArgs e)
	{
		base.OnDeactivated(e);

		this.ContextMenu.IsOpen = false;
	}

	protected virtual void OnContextMenuClosed(object sender, RoutedEventArgs e)
	{
		this.Hide();
	}

	public event EventHandler<MenuAction> Selected;

	private void ShowSettingsClick(object sender, RoutedEventArgs e) => Selected?.Invoke(this, MenuAction.ShowSettings);
	private void RecheckClick(object sender, RoutedEventArgs e) => Selected?.Invoke(this, MenuAction.Recheck);
	private void EnableClick(object sender, RoutedEventArgs e) => Selected?.Invoke(this, MenuAction.Enable);
	private void DisableClick(object sender, RoutedEventArgs e) => Selected?.Invoke(this, MenuAction.Disable);
	private void CloseAppClick(object sender, RoutedEventArgs e) => Selected?.Invoke(this, MenuAction.CloseApp);
}

public enum MenuAction
{
	None = 0,
	ShowSettings,
	Recheck,
	Enable,
	Disable,
	CloseApp
}