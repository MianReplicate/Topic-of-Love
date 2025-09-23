namespace Topic_of_Love.Mian;

public class CustomStatsRows : StatsRowsContainer
{
    public void OnEnable()
    {
        
    }

    public void beginShow()
    {
        this.StartCoroutine(this.showRows());
    }
}