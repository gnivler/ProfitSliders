using HarmonyLib;
using System;
using System.Reflection;
using App.Common;
using HarmonyLib.Tools;
using QxFramework.Utilities;
using UnityEngine.UI;

// ReSharper disable InconsistentNaming

namespace ProfitSliders;

internal class Patches
{
    private static readonly MethodInfo method = typeof(GameMgr).GetMethod("Get", []);
    private static readonly Type iCity = AccessTools.TypeByName("ICityManager");
    private static readonly MethodInfo genericMethod = method?.MakeGenericMethod(iCity);
    private static readonly CityManager cityMgr = (CityManager)genericMethod.Invoke(null, null);
    private static void Log(object input) => HarmonyFileLog.Writer?.Write(input.ToString());

    // the audio grinds on my neurons
    // [HarmonyPatch(typeof(SoundMain), "SetState")]
    // public static void Postfix(SoundMain __instance)
    // {
    //     __instance.transform.Find("StateSound/Danger").GetComponent<AudioSource>().volume = 0;
    // }

    // if a 3rd becomes needed maybe look at writing a collection processor
    [HarmonyPatch(typeof(PurchasePanel), "OnDisplay")]
    public static void Postfix(PurchasePanel __instance, bool ___isBuy, Goods ___goods)
    {
        var piece = (float)AccessTools.PropertyGetter(typeof(PurchasePanel), "SinglePiece").Invoke(__instance, []);
        SliderToBestPrice(__instance, ___isBuy, ___goods, piece);
    }

    [HarmonyPatch(typeof(CaravanPurchasePanel), "OnDisplay")]
    public static void Postfix(CaravanPurchasePanel __instance, bool ___isBuy, Goods ___goods)
    {
        var piece = (float)AccessTools.PropertyGetter(typeof(CaravanPurchasePanel), "SinglePiece").Invoke(__instance, []);
        SliderToBestPrice(__instance, ___isBuy, ___goods, piece);
    }

    private static void SliderToBestPrice(SliderUI panel, bool isBuy, Goods goods, float unitCost)
    {
        var sliderGo = panel.gameObject.transform.FindDescendant("Slider").gameObject;
        var slider = sliderGo.GetComponent<Slider>();
        int numToTrade = default, myStock = default, traderStock = default;
        float best = default;
        var itemMgr = GameMgr.Get<IItemManager>();
        var marketMgr = GameMgr.Get<IMarketManager>();

        switch (panel)
        {
            case PurchasePanel:
                myStock = itemMgr.GetGoodsCout(goods.Object.Id);
                traderStock = goods.Count;
                break;
            case CaravanPurchasePanel:
                var caravanArgs = (CaravanTradeArgs)AccessTools.Field(typeof(CaravanPurchasePanel), "Args").GetValue(panel);
                var goodsStore = (GoodsStore)caravanArgs.selfCity.Modules.Find(m => m is GoodsStore);
                myStock = goodsStore.storeData.GetItemCount(goods.Object.Id);
                var goodsList = marketMgr.GetMarket(caravanArgs.tradeCity.CityId, caravanArgs.mapID).GoodsList;
                Log($"GoodsList? {goodsList}, goods.Id: {goods.Id}");
                traderStock = marketMgr.GetMarket(caravanArgs.tradeCity.CityId, caravanArgs.mapID).GoodsList.Find(g => g.Id == goods.Id).Count;
                break;
        }

        var howManyGoodsToCheck = isBuy ? traderStock : myStock;
        for (var i = 1; i <= howManyGoodsToCheck; i++)
        {
            var stock = isBuy ? goods.Count - i + 1 : goods.Count + i - 1;
            var price = marketMgr.GetPrice(goods.Id, stock, isBuy ? 1 : -1, isBuy);
            Log($"GetPrice returned {price} for {i} {goods} with city stock at {stock} (Buy? {isBuy})");
            var valToCompare = isBuy ? Math.Max(price, unitCost) : Math.Min(price, unitCost);
            var profit = (goods.Price - valToCompare) * i;
            if (!isBuy)
                profit = Math.Abs(profit);

            if (profit < 0
                || price < unitCost && isBuy
                || price > unitCost && !isBuy
                || profit < best)
                break;

            best = profit;
            numToTrade = i;
            Log($"{i} => Price: {price}, Profit: {profit}, Volume: {numToTrade}/{howManyGoodsToCheck}");
        }

        var percent = (float)numToTrade / howManyGoodsToCheck;
        slider.SetValueWithoutNotify(percent);
    }

    // slight vanilla rewrite avoids trying to get cityData when we don't even need it (and it throws, missing a null check)
    [HarmonyPatch(typeof(MarketManager), "GetPrice", typeof(int), typeof(int), typeof(int), typeof(bool))]
    public static bool Prefix(int id, int citycount, int playercount, bool IsBuy, MarketManager __instance, ref float __result)
    {
        if (genericMethod == null) return false;
        var flag1 = false;
        var flag2 = false;
        var num1 = 1f;
        if (cityMgr.TryGetCityModule<SpecialGoods>(id, out var cityModule1) && cityModule1.SpecialGoodsType.Contains(id))
            flag1 = true;
        if (cityMgr.TryGetCityModule<LackGoods>(id, out var cityModule2) && cityModule2.LackGoodsType.Contains(id))
            flag2 = true;
#pragma warning disable Harmony003
        var num2 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "LowValue");
        var num3 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "HighValue");
        var num4 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "HighPrice");
        var num5 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "LowPrice");
        var num6 = IsBuy ? 1.1f : 1f;
        if (flag1)
        {
            num2 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "LowMinValue");
            num3 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "HighValue");
            num4 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "HighPrice");
            num5 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "MinPrice");
            num1 = IsBuy ? 0.8f : 0.75f;
        }
        else if (flag2)
        {
            num2 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "LowValue");
            num3 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "HighMaxValue");
            num4 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "MaxPrice");
            num5 = MonoSingleton<Data>.Instance.TableAgent.GetFloat("Shop", id.ToString(), "LowPrice");
#pragma warning restore Harmony003
            num1 = IsBuy ? 1.25f : 1.2f;
        }

        __result = (citycount - playercount >= (double)num2
            ? citycount - playercount <= (double)num3
                ? (float)((num4 - (double)num5) / (num2 - (double)num3) * (citycount - playercount - (double)num2)) + num4
                : num5
            : num4) * num6 * num1;

        return false;
    }
}