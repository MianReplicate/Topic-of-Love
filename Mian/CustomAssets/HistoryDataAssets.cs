
using db;

namespace Topic_of_Love.Mian.CustomAssets;

public class HistoryDataAssets
{
    public class CustomHistoryDataAsset : HistoryDataAsset
    {
        private string[] categories;

        public string[] Categories
        {
            get => categories;
            set => categories = value;
        }
    }
    
    public static void Init()
    {
        Add(new CustomHistoryDataAsset
        {
            id = "lonely",
            color_hex = "#FB8B8B",
            path_icon = "ui/Icons/status/broke_up",
            statistics_asset = "world_statistics_lonely",
            Categories = new[]{"kingdom"}
        });
    }

    static void Add(CustomHistoryDataAsset asset)
    {
        foreach (var assetCategory in asset.Categories)
        {
            AssetManager.history_meta_data_library.get(assetCategory).categories.Add(asset);
        }

        AssetManager.history_data_library.add(asset);
    }
}