using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>표시 중인 아이콘의 핸들만 소유한다. 재바인딩/비활성화 시 진행 중 로드도 해제한다.</summary>
public sealed class AddressableItemIcon : MonoBehaviour
{
    public Image image;
    public Sprite placeholder;
    private string address;
    private AsyncOperationHandle<IList<IResourceLocation>> locations;
    private AsyncOperationHandle<Sprite> spriteHandle;
    public bool IsLoading { get; private set; }
    public bool HasLoadedSprite => image != null && image.sprite != null && image.sprite != placeholder;

    public void Bind(string value)
    {
        value = value?.Trim() ?? "";
        if (address == value && (IsLoading || HasLoadedSprite)) return;
        Release();
        address = value;
        if (isActiveAndEnabled && !string.IsNullOrEmpty(address)) StartCoroutine(Load(address));
    }

    private void OnEnable()
    {
        ShowPlaceholder();
        if (!string.IsNullOrEmpty(address)) StartCoroutine(Load(address));
    }

    private IEnumerator Load(string key)
    {
        IsLoading = true;
        locations = Addressables.LoadResourceLocationsAsync(key, typeof(Sprite));
        yield return locations;
        if (locations.Status != AsyncOperationStatus.Succeeded || locations.Result == null || locations.Result.Count != 1)
        {
            Debug.LogWarning($"[InventoryIcon] Sprite 주소를 하나로 찾을 수 없습니다: {key}");
            IsLoading = false;
            if (locations.IsValid()) Addressables.Release(locations);
            locations = default;
            yield break;
        }
        spriteHandle = Addressables.LoadAssetAsync<Sprite>(locations.Result[0]);
        yield return spriteHandle;
        if (spriteHandle.Status == AsyncOperationStatus.Succeeded && image != null)
        {
            image.sprite = spriteHandle.Result;
            image.enabled = image.sprite != null;
        }
        else Debug.LogWarning($"[InventoryIcon] 아이콘을 불러오지 못했습니다: {key}");
        IsLoading = false;
        if (locations.IsValid()) Addressables.Release(locations);
        locations = default;
    }

    private void ShowPlaceholder()
    {
        if (image == null) return;
        image.sprite = placeholder;
        image.enabled = placeholder != null;
    }

    private void Release()
    {
        StopAllCoroutines();
        IsLoading = false;
        ShowPlaceholder();
        if (spriteHandle.IsValid()) Addressables.Release(spriteHandle);
        if (locations.IsValid()) Addressables.Release(locations);
        spriteHandle = default;
        locations = default;
    }

    private void OnDisable() => Release();
    private void OnDestroy() => Release();
}
