using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class InventoryTabbedPrefabBuilder
{
    private const string PathRoot = "Assets/Resources/UI/Inventory/DarkInventoryTabbed/";
    private const string PrefabPath = "Assets/Prefabs/InventoryUIController.prefab";

    [MenuItem("Tools/ProjectGame2D/UI/Build Modular Inventory")]
    public static void Build()
    {
        AuthorCategories();
        var outer = Import("inventory_outer_frame_v1", 110);
        var panel = Import("inventory_panel_frame_v1", 70);
        var tab = Import("inventory_bookmark_v1", 180);
        var slot = Import("inventory_slot_v5", 90);
        var quick = Import("inventory_quick_slot_v1", 0);
        var gold = Import("inventory_gold_well_v1", 80);
        var close = Import("inventory_close_v2", 0);
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var board = Find(root, "InventoryPanel");
            Rect(board, 0, 0, 680, 400);
            Skin(board, outer, true);
            Rect(Find(root, "InventoryOpaqueBackdrop"), 0, 0, 642, 358);
            Find(root, "InventoryOpaqueBackdrop").GetComponent<Image>().color = new Color(.65f,.46f,.22f,1);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchoredPosition = Vector2.zero;
            var window = Find(root, "InventoryWindow");
            var equipment = Find(root, "EquipmentPanel");
            equipment.SetParent(board, false);
            Rect(equipment, -184, 22, 250, 258);
            equipment.GetComponent<Image>().color = Color.clear;
            foreach(var old in equipment.GetComponentsInChildren<Image>(true).Where(x=>x.name=="PreviewFrame").ToArray())
                UnityEngine.Object.DestroyImmediate(old.gameObject);
            var previewFrame = Image(equipment, "PreviewFrame", panel, 0, 36, 144, 200, true);
            previewFrame.transform.SetAsFirstSibling();
            Rect(Find(root, "CharacterPreview"), 0, 36, 112, 176);
            string[] names = {"HeadSlot","BodySlot","FootSlot","WeaponSlot","ShieldSlot","NecklaceSlot","RingSlot"};
            Vector2[] pos = {new(-100,100),new(-100,48),new(-100,-4),new(100,100),new(100,48),new(100,-4),new(100,-56)};
            for(int i=0;i<names.Length;i++)
            {
                var t=Find(root,names[i]); Rect(t,pos[i].x,pos[i].y,42,42); Skin(t,slot,false);
            }
            var stats = Find(root,"InventoryStats");
            Rect(stats,-184,-120,250,90); Skin(stats,panel,true);
            stats.SetAsLastSibling();
            var gridFrame = Find(root,"GridFrame");
            gridFrame.SetParent(board,false); Rect(gridFrame,126,10,370,246); Skin(gridFrame,panel,true);
            gridFrame.SetAsFirstSibling();
            var scroll=Find(root,"GridScrollView");
            Rect(scroll,126,10,334,222);
            var layout=Find(root,"GridSlot").GetComponent<GridLayoutGroup>();
            layout.cellSize=new Vector2(40,40); layout.spacing=new Vector2(18,5);
            layout.padding=new RectOffset(3,3,0,0);
            foreach(var cell in Find(root,"GridSlot").GetComponentsInChildren<InventorySlotUI>(true))
            {
                Skin(cell.transform,slot,false);
                var icon=cell.transform.Find("Icon") as RectTransform;
                icon.anchorMin=new Vector2(.17f,.12f); icon.anchorMax=new Vector2(.83f,.88f);
                icon.offsetMin=icon.offsetMax=Vector2.zero;
            }
            var title=Find(root,"TitleInventory"); Rect(title,0,179,142,24);
            title.GetComponent<Image>().color=Color.white;
            var closeT=Find(root,"CloseBtn"); Rect(closeT,307,176,26,26); Skin(closeT,close,false);
            closeT.GetComponent<Image>().raycastTarget=true; closeT.SetAsLastSibling();
            var font=Find(root,"GoldText").GetComponent<TMP_Text>().font;
            Remove(board,"CategoryBookmarks");
            var tabs=new GameObject("CategoryBookmarks",typeof(RectTransform)); tabs.transform.SetParent(board,false);
            Rect(tabs.transform,126,151,370,30);
            string[] labels={"ALL","EQUIPMENT","RESOURCES","CONSUMPTION","OTHERS"};
            InventoryCategory[] cats={InventoryCategory.All,InventoryCategory.Equipment,InventoryCategory.Resources,InventoryCategory.Consumption,InventoryCategory.Others};
            var inventory=root.GetComponentInChildren<InventoryUI>(true);
            for(int i=0;i<5;i++)
            {
                var bg=Image(tabs.transform,labels[i],tab,-148+i*74,0,72,28,true);
                bg.pixelsPerUnitMultiplier=18;
                bg.raycastTarget=true;
                var button=bg.gameObject.AddComponent<Button>(); button.targetGraphic=bg;
                var colors=button.colors; colors.highlightedColor=new Color(.8f,.93f,1); colors.pressedColor=new Color(.5f,.7f,.9f); button.colors=colors;
                var label=Text(bg.transform,"Label",font,labels[i],i==2?7.5f:8.5f,0,0,62,18);
                var focus=Image(bg.transform,"SelectedUnderline",null,0,-10,40,2,false);
                focus.color=new Color(.15f,.7f,1); focus.enabled=i==0;
                var handler=bg.gameObject.AddComponent<InventoryCategoryTabUI>();
                var so=new SerializedObject(handler);
                so.FindProperty("_inventory").objectReferenceValue=inventory;
                so.FindProperty("_category").intValue=(int)cats[i];
                so.FindProperty("_focus").objectReferenceValue=focus;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            Remove(board,"QuickAssign");
            var quickRoot=new GameObject("QuickAssign",typeof(RectTransform)); quickRoot.transform.SetParent(board,false);
            Rect(quickRoot.transform,75,-146,240,64);
            Text(quickRoot.transform,"Header",font,"QUICK ASSIGN",8,0,26,240,14);
            for(int i=0;i<4;i++)
            {
                var bg=Image(quickRoot.transform,"QuickSlot"+(i+1),quick,-87+i*58,-3,48,48,false); bg.raycastTarget=true;
                var selection=Image(bg.transform,"Selection",quick,0,0,48,48,false); selection.color=new Color(.25f,.7f,1,.4f); selection.enabled=i==0;
                var icon=Image(bg.transform,"Icon",null,0,0,25,25,false); icon.preserveAspect=true; icon.enabled=false;
                var qty=Text(bg.transform,"Quantity",font,"",8,9,-10,22,12); qty.alignment=TextAlignmentOptions.Right;
                Text(bg.transform,"Key",font,(i+1).ToString(),8,0,-26,20,12);
                bg.gameObject.AddComponent<QuickBarSlotUI>().Configure(i,icon,qty,selection);
            }
            var currency=Find(root,"CurrencyRow"); Rect(currency,246,-146,115,40);
            var currencyLayout=currency.GetComponent<HorizontalLayoutGroup>(); if(currencyLayout!=null) currencyLayout.enabled=false;
            var goldT=Find(root,"Gold"); Rect(goldT,0,0,115,40);
            var badge=Find(root,"GoldBadge"); Skin(badge,gold,true);
            var badgeRect=(RectTransform)badge; badgeRect.anchorMin=Vector2.zero; badgeRect.anchorMax=Vector2.one; badgeRect.offsetMin=badgeRect.offsetMax=Vector2.zero;
            Rect(Find(root,"GoldIcon"),-30,0,18,18);
            Rect(Find(root,"GoldText"),15,0,56,20);
            Find(root,"GoldText").GetComponent<TMP_Text>().fontSize=10;
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ApplyApprovedCompleteFrame();
        SyncGameplayInstance();
        AssetDatabase.SaveAssets();
    }

    // Owner-approved complete background: preserve its internal frame geometry, overlay only content.
    private static void ApplyApprovedCompleteFrame()
    {
        var path=PathRoot+"inventory_complete_frame_approved.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spriteBorder=Vector4.zero; importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.maxTextureSize=2048;
        importer.SaveAndReimport();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var board=Find(root,"InventoryPanel");
            Rect(board,0,0,680,680*941f/1672f);
            Skin(board,AssetDatabase.LoadAssetAtPath<Sprite>(path),false);
            ((RectTransform)Find(root,"InventoryWindow")).anchoredPosition=Vector2.zero;
            var bakedNames=new[]{"InventoryOpaqueBackdrop","PreviewFrame","GridFrame","InventoryStats","GoldBadge","TitleInventory"};
            foreach(var image in root.GetComponentsInChildren<Image>(true))
                if(bakedNames.Contains(image.name)) image.enabled=false;
            var backdrop=Find(root,"InventoryOpaqueBackdrop");
            backdrop.SetParent(Find(root,"InventoryWindow"),false); backdrop.SetAsFirstSibling();
            Rect(backdrop,0,-4,630,320);
            var backdropImage=backdrop.GetComponent<Image>();
            backdropImage.enabled=true; backdropImage.sprite=null; backdropImage.type=UnityEngine.UI.Image.Type.Simple;
            backdropImage.color=new Color(.075f,.045f,.025f,1); backdropImage.raycastTarget=false;
            var equipment=Find(root,"EquipmentPanel"); Rect(equipment,0,0,680,378);
            equipment.GetComponent<Image>().raycastTarget=false;
            Find(root,"CharacterPreview").GetComponent<RawImage>().raycastTarget=false;
            Rect(Find(root,"CharacterPreview"),-183,49,132,174);
            Remove(board,"InventoryTitleLabel");
            Text(board,"InventoryTitleLabel",Find(root,"GoldText").GetComponent<TMP_Text>().font,
                "INVENTORY",13,-180,161,210,18);
            string[] names={"HeadSlot","BodySlot","FootSlot","WeaponSlot","ShieldSlot","NecklaceSlot","RingSlot"};
            float[] px={145,145,145,633,633,633,633}; float[] py={182,303,425,182,303,425,546};
            for(int i=0;i<names.Length;i++)
            {
                var t=Find(root,names[i]); Rect(t,(px[i]-844)*680/1688,(469-py[i])*680/1688,40,40);
                t.GetComponent<Image>().color=Color.clear; // Keep raycast/drag components, background is baked.
                t.GetComponent<Image>().raycastTarget=true;
                foreach(var graphic in t.GetComponentsInChildren<Graphic>(true))
                    if(graphic.transform!=t) graphic.raycastTarget=false;
            }
            foreach(var cell in root.GetComponentsInChildren<InventorySlotUI>(true))
                foreach(var graphic in cell.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget=graphic.transform==cell.transform;
            Rect(Find(root,"InventoryStats"),-183,-102,244,84);
            Rect(Find(root,"GridScrollView"),126,10,326,210);
            var layout=Find(root,"GridSlot").GetComponent<GridLayoutGroup>();
            layout.spacing=new Vector2(16,2); layout.cellSize=new Vector2(40,40);
            var tabs=Find(root,"CategoryBookmarks"); Rect(tabs,0,0,680,378);
            string[] labels={"ALL","EQUIPMENT","RESOURCES","CONSUMPTION","OTHERS"};
            for(int i=0;i<5;i++)
            {
                var t=tabs.Find(labels[i]); Rect(t,(-10+i*69),136,62,24);
                t.GetComponent<Image>().color=Color.clear;
            }
            var quick=Find(root,"QuickAssign"); Rect(quick,0,0,680,378);
            quick.Find("Header").gameObject.SetActive(false);
            for(int i=0;i<4;i++)
            {
                var t=quick.Find("QuickSlot"+(i+1)); Rect(t,-12+i*58,-129,48,48);
                t.GetComponent<Image>().color=Color.clear;
                Rect(t.Find("Key"),0,-15,20,12);
                Rect(t.Find("Quantity"),9,-5,16,10);
            }
            Rect(Find(root,"CurrencyRow"),247,-129,90,22);
            Rect(Find(root,"Gold"),0,0,90,22);
            Rect(Find(root,"GoldIcon"),-27,0,15,15);
            Rect(Find(root,"GoldText"),13,0,54,20);
            Rect(Find(root,"CloseBtn"),287,161,22,22);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    private static void SyncGameplayInstance()
    {
        const string path="Assets/Prefabs/UI/GameplayUIRoot.prefab";
        var gameplay=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var inventory=Find(gameplay,"InventoryUIController").gameObject;
            var modifications=PrefabUtility.GetPropertyModifications(inventory);
            if(modifications!=null)
                PrefabUtility.SetPropertyModifications(inventory,modifications.Where(m=>
                    !IsInventoryGeometryOverride(m)).ToArray());
            PrefabUtility.SaveAsPrefabAsset(gameplay,path);
        }
        finally {PrefabUtility.UnloadPrefabContents(gameplay);}
    }

    private static bool IsInventoryGeometryOverride(PropertyModification modification)
    {
        var component=modification.target as Component;
        if (!(component is RectTransform) && !(component is Image)) return false;
        for(var parent=component.transform;parent!=null;parent=parent.parent)
            if(parent.name=="InventoryUIController") return true;
        return false;
    }

    private static void AuthorCategories()
    {
        foreach(var guid in AssetDatabase.FindAssets("t:ItemSO",new[]{"Assets/Resources/Items"}))
        {
            var item=AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
            if(item==null || item.inventoryCategory!=InventoryCategory.All) continue;
            if(item is EquipmentItemSO)
                item.inventoryCategory=InventoryCategory.Equipment;
            else if(item is SeedItemSO || item is FishingBaitItemSO)
                item.inventoryCategory=InventoryCategory.Others;
            else if(item.type==ItemType.Material)
                item.inventoryCategory=InventoryCategory.Resources;
            if(item.inventoryCategory!=InventoryCategory.All) EditorUtility.SetDirty(item);
        }
        // Explicit initial content authoring. Runtime filtering reads metadata only.
        string[] foods={"Food/Bread","Food/Cheese","Drinks/Water","Drinks/Ale","Farming/Crop.Carrot","Farming/Crop.Eggplant"};
        string[] potions={"Shop/HealthPotion","Consumables/BuffPotionAttack","Consumables/BuffPotionDefense"};
        foreach(var path in foods) SetCategory(path,InventoryCategory.Consumption);
        foreach(var path in potions) SetCategory(path,InventoryCategory.Consumption);
    }
    private static void SetCategory(string path,InventoryCategory category)
    {
        var item=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/Resources/Items/"+path+".asset");
        if(item!=null && item.inventoryCategory!=category) {item.inventoryCategory=category;EditorUtility.SetDirty(item);}
    }

    public static string RenderPreview()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var cameraObject=new GameObject("InventoryPreviewCamera",typeof(Camera));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.GetComponent<Camera>();
        camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
        camera.orthographic=true;camera.orthographicSize=540;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.32f,.42f,.31f);
        var target=new RenderTexture(1920,1080,24);camera.targetTexture=target;
        var canvasObject=new GameObject("InventoryPreviewCanvas",typeof(RectTransform),typeof(Canvas));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject,scene);
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        var canvasRect=(RectTransform)canvasObject.transform;canvasRect.sizeDelta=new Vector2(800,450);
        canvasRect.localScale=Vector3.one*2.4f;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        instance.transform.SetParent(canvas.transform,false);
        Find(instance,"InventoryWindow").gameObject.SetActive(true);
        Find(instance,"ItemTooltip").gameObject.SetActive(false);
        var preview=Find(instance,"CharacterPreview").GetComponent<RawImage>();
        if(preview.texture==null) preview.color=Color.clear;
        var items=AssetDatabase.FindAssets("t:ItemSO",new[]{"Assets/Resources/Items"})
            .Select(g=>AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(i=>i!=null && i.icon!=null).ToArray();
        var cells=instance.GetComponentsInChildren<InventorySlotUI>(true);
        for(int i=0;i<cells.Length;i++) cells[i].SetSlot(i<30?new InventorySlot{item=items[i%items.Length],quantity=i%9+1}:null);
        Find(instance,"GoldText").GetComponent<TMP_Text>().text="12500";
        string[] values={"100/100","120.5","42.0","12.0","25%","100/100"};
        string[] stats={"HP","ATK","DEF","SPD","CRIT","STA"};
        for(int i=0;i<6;i++) Find(instance,stats[i]+"Value").GetComponent<TMP_Text>().text=values[i];
        foreach(var text in instance.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Find(instance,"GridSlot"));
        camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var output=new Texture2D(1920,1080,TextureFormat.RGBA32,false);output.ReadPixels(new Rect(0,0,1920,1080),0,0);output.Apply();
        Directory.CreateDirectory("Assets/Documentation/DevelopmentPlan/InventoryRedesignQA");
        var path="Assets/Documentation/DevelopmentPlan/InventoryRedesignQA/Inventory1920x1080.png";
        File.WriteAllBytes(path,output.EncodeToPNG());RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(output);camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);
        UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        return path;
    }

    private static Sprite Import(string name,int border)
    {
        // Tight alpha crop is asset preparation; originals stay untouched for provenance.
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
        texture.LoadImage(File.ReadAllBytes(PathRoot+name+".png"));
        var pixels=texture.GetPixels32(); int x0=texture.width,y0=texture.height,x1=0,y1=0;
        for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
            if(pixels[y*texture.width+x].a>16) {x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
        var cropped=new Texture2D(x1-x0+1,y1-y0+1,TextureFormat.RGBA32,false);
        cropped.SetPixels(texture.GetPixels(x0,y0,cropped.width,cropped.height)); cropped.Apply();
        Directory.CreateDirectory(PathRoot+"Processed");
        var path=PathRoot+"Processed/"+name+".png";
        File.WriteAllBytes(path,cropped.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(cropped);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100; importer.spriteBorder=new Vector4(border,border,border,border);
        importer.filterMode=FilterMode.Point; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
        importer.textureCompression=TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static Transform Find(GameObject root,string name) => root.GetComponentsInChildren<Transform>(true).First(t=>t.name==name);
    private static void Remove(Transform parent,string name) {var t=parent.Find(name);if(t!=null)UnityEngine.Object.DestroyImmediate(t.gameObject);}
    private static void Rect(Transform t,float x,float y,float w,float h)
    {
        var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);
    }
    private static void Skin(Transform t,Sprite sprite,bool sliced)
    {
        var im=t.GetComponent<Image>()??t.gameObject.AddComponent<Image>();im.sprite=sprite;
        im.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
        im.pixelsPerUnitMultiplier=5;im.color=Color.white;im.raycastTarget=false;
    }
    private static Image Image(Transform parent,string name,Sprite sprite,float x,float y,float w,float h,bool sliced)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        Rect(go.transform,x,y,w,h);Skin(go.transform,sprite,sliced);return go.GetComponent<Image>();
    }
    private static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,string value,float size,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        Rect(go.transform,x,y,w,h);var text=go.GetComponent<TMP_Text>();text.font=font;text.text=value;text.fontSize=size;
        text.alignment=TextAlignmentOptions.Center;text.color=new Color(1,.94f,.79f);text.raycastTarget=false;
        text.enableAutoSizing=true;text.fontSizeMin=size-1;text.fontSizeMax=size;text.overflowMode=TextOverflowModes.Ellipsis;
        return text;
    }
}
