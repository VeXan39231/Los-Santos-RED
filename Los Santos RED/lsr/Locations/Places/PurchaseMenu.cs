﻿using ExtensionsMethods;
using LosSantosRED.lsr.Helper;
using LosSantosRED.lsr.Interface;
using LSR.Vehicles;
using Rage;
using Rage.Native;
using RAGENativeUI;
using RAGENativeUI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

public class PurchaseMenu : Menu
{
    private UIMenu ParentMenu;
    private MenuPool MenuPool;
    private IModItems ModItems;
    private ILocationInteractable Player;
    private UIMenu purchaseMenu;
    private ISettingsProvideable Settings;
    private Camera StoreCam;
    private ITimeControllable Time;
    private IWeapons Weapons;
    private IEntityProvideable World;
    private Transaction Transaction;
    private ShopMenu ShopMenu;
    public PurchaseMenu(MenuPool menuPool, UIMenu parentMenu, ShopMenu shopMenu, Transaction transaction, IModItems modItems, ILocationInteractable player, IEntityProvideable world, ISettingsProvideable settings, IWeapons weapons, ITimeControllable time)
    {
        ModItems = modItems;
        ParentMenu = parentMenu;
        Player = player;
        World = world;
        Settings = settings;
        MenuPool = menuPool;
        Weapons = weapons;
        Time = time;
        ShopMenu = shopMenu;
        Transaction = transaction;
    }
    public void Dispose()
    {
        Hide();
        Game.RawFrameRender -= (s, e) => MenuPool.DrawBanners(e.Graphics);
    }
    public override void Hide()
    {
        Transaction.ClearPreviews();
        if (purchaseMenu != null)
        {
            purchaseMenu.Visible = false;
        }
        Player.ButtonPrompts.Clear();
    }
    public void Setup()
    {
        EntryPoint.WriteToConsole($"PurchaseMenu: HasBannerImage {Transaction.HasBannerImage} RemoveBanner {Transaction.RemoveBanner}");
        StoreCam = Camera.RenderingCamera;
        if (ParentMenu != null)
        {
            purchaseMenu = MenuPool.AddSubMenu(ParentMenu, "Buy");
            if (Transaction.HasBannerImage)
            {
                purchaseMenu.SetBannerType(Transaction.BannerImage);
            }
            else if (Transaction.RemoveBanner)
            {
                purchaseMenu.RemoveBanner();
            }
            purchaseMenu.OnIndexChange += (sender, newIndex) =>
            {
                GeneratePreview(purchaseMenu, newIndex);
            };
            purchaseMenu.OnMenuOpen += (sender) =>
            {
                GeneratePreview(purchaseMenu, purchaseMenu.CurrentSelection);
            };
            purchaseMenu.OnMenuClose += (sender) =>
            {
                Transaction.ClearPreviews();
            };
        }
        if (purchaseMenu == null)
        {
            return;
        }
        purchaseMenu.Clear();
        CreateCategories();
        foreach (MenuItem cii in ShopMenu.Items.Where(x=> x.Purchaseable))
        {
            EntryPoint.WriteToConsole($"PURCHASE MENU ADD ITEM {cii.ModItemName} Purchaseable:{cii.Purchaseable} PurchasePrice {cii.PurchasePrice} NumberOfItemsToSellToPlayer:{cii.NumberOfItemsToSellToPlayer} NumberOfItemsToPurchaseFromPlayer:{cii.NumberOfItemsToPurchaseFromPlayer}");
            cii.ModItem.CreatePurchaseMenuItem(Transaction, cii, purchaseMenu, Settings, Player, Transaction.IsStealing, World);
        }
    }
    public override void Show()
    {
        if (purchaseMenu != null)
        {
            purchaseMenu.Visible = true;
        }
    }
    public override void Toggle()
    {
        if (!purchaseMenu.Visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    public void Update()
    {

    }
    private void CreateCategories()
    {
        if (ShopMenu.Items.Where(x => x.Purchaseable).Count() < 7)
        {
            return;
        }
        List<string> Categories = new List<string>();
        foreach (MenuItem cii in ShopMenu.Items.Where(x => x.Purchaseable))
        {
            if (!Categories.Contains(cii.ModItem.MenuCategory))
            {
                Categories.Add(cii.ModItem.MenuCategory);
            }
        }
        foreach(string category in Categories)
        {
            UIMenu categoryMenu = MenuPool.AddSubMenu(purchaseMenu, category);
            if (Transaction.HasBannerImage)
            {
                categoryMenu.SetBannerType(Transaction.BannerImage);
            }
            else if (Transaction.RemoveBanner)
            {
                categoryMenu.RemoveBanner();
            }
            categoryMenu.OnIndexChange += (sender, newIndex) =>
            {
                GeneratePreview(categoryMenu, newIndex);                
            };
            categoryMenu.OnMenuOpen += (sender) =>
            {
                GeneratePreview(categoryMenu, categoryMenu.CurrentSelection);
            };
            categoryMenu.OnMenuClose += (sender) =>
            {
                Transaction.ClearPreviews();
            };
        }
    }
    private void GeneratePreview(UIMenu menuSelected, int v)
    {
        if(menuSelected == null || menuSelected.MenuItems == null)
        {
            Transaction.ClearPreviews();
            EntryPoint.WriteToConsole($"{menuSelected.TitleText} Menu OnIndexChange newIndex {v} NO ITEM SELECTED");
            return;
        }
        UIMenuItem myItem = menuSelected.MenuItems.ElementAtOrDefault(v);
        if (myItem == null)
        {
            Transaction.ClearPreviews();
            EntryPoint.WriteToConsole($"{menuSelected.TitleText} Menu OnIndexChange newIndex {v} NO ITEM SELECTED");
            return;
        }
        MenuItem selectedMenuItem = ShopMenu.Items.Where(x => x.ModItemName == myItem.Text).FirstOrDefault();
        if (selectedMenuItem != null)
        {
            CreatePreview(selectedMenuItem);
            EntryPoint.WriteToConsole($"{menuSelected.TitleText} Menu OnIndexChange newIndex {v} SELECTED {selectedMenuItem.ModItemName}");
        }
        else
        {
            Transaction.ClearPreviews();
            EntryPoint.WriteToConsole($"{menuSelected.TitleText} Menu OnIndexChange newIndex {v} NO ITEM SELECTED");
        }
    }
    private void CreatePreview(MenuItem selectedMenu)
    {
        Transaction.ClearPreviews();
        if (selectedMenu == null || selectedMenu.ModItem == null || !Transaction.PreviewItems || !Settings.SettingsManager.PlayerOtherSettings.GenerateStoreItemPreviews)
        {
            return;
        }




        GameFiber.StartNew(delegate
        {
            try
            {
                selectedMenu.ModItem.CreatePreview(Transaction, StoreCam);
            }
            catch (Exception ex)
            {
                EntryPoint.WriteToConsole(ex.Message + " " + ex.StackTrace, 0);
                EntryPoint.ModController.CrashUnload();
            }
        }, "CreatePreview");





    }

    public void OnItemSold(MenuItem menuItem)
    {
        menuItem.ModItem.UpdatePurchaseMenuItem(Transaction, menuItem, Settings, Player, Transaction.IsStealing);
    }
}