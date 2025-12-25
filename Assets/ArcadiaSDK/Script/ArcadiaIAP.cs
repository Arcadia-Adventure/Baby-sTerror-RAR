using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
public class ArcadiaIAP : MonoBehaviour
{
    public IAPType iAPType;
    public TMPro.TMP_Text priceTxt;
    
    void OnEnable()
    {
        if(iAPType == IAPType.removeAds && PlayerPrefs.GetInt("removeAds") == 1)
        {
            gameObject.SetActive(false);
            return;
        }
        GetComponent<CodelessIAPButton>().onProductFetched.AddListener(UpdatePriceText);
        GetComponent<CodelessIAPButton>().onOrderConfirmed.AddListener(OnPurchase);
    }
    void OnDisable()
    {
        GetComponent<CodelessIAPButton>().onProductFetched.RemoveListener(UpdatePriceText);
        GetComponent<CodelessIAPButton>().onOrderConfirmed.RemoveListener(OnPurchase);
    }
    public void OnPurchase(ConfirmedOrder order)
    {
        switch (iAPType)
        {
            case IAPType.removeAds:
                ArcadiaSdkManager.Agent.OnRemoveAds();
                gameObject.SetActive(false);
            break;
        }
    }
    public void UpdatePriceText(Product product)
    {
        priceTxt.text=product.metadata.isoCurrencyCode+product.metadata.localizedPriceString;
    }
    public enum IAPType{other, removeAds}
}
