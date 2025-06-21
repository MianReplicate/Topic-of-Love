namespace Topic_of_Love.Mian.CustomAssets;

public class StatisticAssets
{
    public static void Init()
    {
        Add(new StatisticsAsset
        {
            id = "world_statistics_lonely",
            path_icon = "ui/Icons/status/broke_up",
            is_world_statistics = true,
            list_window_meta_type = MetaType.Unit,
            long_action = _ => World.world.world_object.countLonely(),
            world_stats_tabs = WorldStatsTabs.General
        });
    }

    static void Add(StatisticsAsset asset)
    {
        AssetManager.statistics_library.add(asset);
    }
}