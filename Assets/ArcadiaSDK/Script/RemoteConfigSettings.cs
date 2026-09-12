using System;

[Serializable]
public class AdsRemoteSettings
{
    public bool precache = true;
    public bool app_open = true;
    public bool interstitial = true;
    public bool rewarded = true;
    public bool banner = true;
}

[Serializable]
public class GameRemoteSettings
{
    public bool require_internet = true;
    public bool show_update_on_start = true;
    public int rate_us_level = 1;
}
