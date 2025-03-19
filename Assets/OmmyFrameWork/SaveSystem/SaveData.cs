//Ommy
// Introduce your new variables under Game Variables and pass them accordingly
// in the Constructor => SaveData();
using UnityEngine;
using System.Collections.Generic;
namespace Ommy.SaveData
{
public sealed class SaveData
{
    //===================================================
    // PRIVATE FIELDS
    //===================================================
    private static SaveData _instance = null;

    //===================================================
    // PROPERTIES
    //===================================================
    public static SaveData Instance
    {
        get
        {
            if(_instance is null)
            {
                _instance = new SaveData();
                SaveSystem.LoadProgress();
            }//if end
            return _instance;
        }//get end
    }//Property end

    //===================================================
    // FIELDS
    //===================================================

    public bool Music  = true;

    public bool SFX    = true;

    public bool Haptic = true;
    [Space]
    public int Level  = 1;
    public int Coins  = 0;
    public int Gems = 0;
    public int GameTutorial = 0;
    public int CurrentGameTutorialStep = 0;

    [HideInInspector]
    public string HashOfSaveData = null;

   // public List<WorldSavableData> worldSavableData = new List<WorldSavableData>();
    public int playerFireRateUpgradeIndex=0, playerDamageRateUpgradeIndex = 0, currentWeaponIndex=0;
   // public int CurrentWorldIndex = 0;

    public SaveData(){}

    private SaveData(SaveData data)
    {
        Music  = data.Music;
        SFX    = data.SFX;
        Haptic = data.Haptic;

        Level  = data.Level;
        Coins  = data.Coins;
        playerFireRateUpgradeIndex = data.playerFireRateUpgradeIndex;
        playerDamageRateUpgradeIndex = data.playerDamageRateUpgradeIndex;
        currentWeaponIndex = data.currentWeaponIndex;
        Gems = data.Gems;
        GameTutorial = data.GameTutorial;
        CurrentGameTutorialStep = data.CurrentGameTutorialStep;
   //     CurrentWorldIndex = data.CurrentWorldIndex;

    }//CopyConstructor() end

    public SaveData CreateSaveObject() => new SaveData(_instance);

    public void Reset() => _instance = new SaveData();

}//class end
}