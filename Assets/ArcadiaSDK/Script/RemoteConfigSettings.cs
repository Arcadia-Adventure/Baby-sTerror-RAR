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

    // Seconds since the last full-screen ad (interstitial, rewarded or app open) closed.
    public int interstitial_cooldown = 0;
    // 0 = unlimited.
    public int interstitial_max_per_session = 0;
    public int interstitial_start_level = 0;
    // Shows on every Nth request that passes the other caps.
    public int interstitial_every_n = 1;

    public bool banner_main_menu = true;
    public bool banner_level_select = true;
    public bool banner_gameplay = true;
}

[Serializable]
public class GameRemoteSettings
{
    public bool require_internet = true;
    public bool show_update_on_start = true;
    public int rate_us_level = 1;
}
