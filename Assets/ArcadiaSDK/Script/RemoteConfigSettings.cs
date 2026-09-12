using System;

[Serializable]
public class AdsRemoteSettings
{
    public bool precache = false;
    public bool app_open = false;
    public bool interstitial = true;
    public bool rewarded = true;
    public bool banner = true;
    public int ad_load_timeout = 8;
}

[Serializable]
public class GameRemoteSettings
{
    public bool require_internet = true;
    public bool show_update_on_start = true;
    public int rate_us_level = 1;
}
