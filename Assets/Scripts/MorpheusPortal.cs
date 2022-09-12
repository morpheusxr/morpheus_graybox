using Mirror;
using System.Collections;
using UnityEngine;

public class MorpheusPortal : NetworkBehaviour
{
#if !CONTENT_CREATOR
    public string portalName;
    public string targetZoneId;
    public string targetPortalName;

    public bool isTeleportUser;
    public bool isPlayingUserEffect;

    private MeshRenderer portalRenderer;
    private Color defaultColor;

    const float userTeleportationEffectTime = 1;

    public IEnumerator Start()
    {
#if !UNITY_SERVER
        if (string.IsNullOrEmpty(portalName)) portalName = GetComponent<PortalInfo>().name;

        var portalInfo = GetComponent/*InParent*/<PortalInfo>();
        portalRenderer = portalInfo.GetComponentInChildren<MeshRenderer>();

        var portalTrigger = portalInfo.handCollider.gameObject.AddComponent<PortalTrigger>();
        portalTrigger.portal = this;

        while (MorpheusUser.LocalUser == null) yield return null;

        var userZoneId = MorpheusUser.LocalUser.ZoneID;

        if (World.TryGetZoneData(userZoneId, out var zoneData))
        {
            if (zoneData.portals.TryGetValue(portalName, out var targetPortal))
            {
                targetZoneId = targetPortal.zoneId;
                targetPortalName = targetPortal.name;

                Debug.Log($"Portal '{portalName}' in zone '{userZoneId}' have targetZoneId = {targetZoneId}, targetPortalName = {targetPortalName}");
            }
            else
            {
                Debug.LogError($"Portal with name '{portalName}' does not exist in zone '{userZoneId}'");
            }
        }
        else
        {
            Debug.LogError($"Zone with id '{userZoneId}' does not exist in this world");
        }

        portalInfo.teleportCollider.gameObject.AddComponent<PortalInteractable>();

        defaultColor = portalRenderer.material.GetColor("_Color");
#else
        yield return null;
#endif
    }

    public void Teleportation()
    {
        isTeleportUser = true;

        //Ёффект телепортации
        CmdTeleportationEffect(MorpheusUser.LocalUser);
    }

    [Command(requiresAuthority = false)]
    private void CmdTeleportationEffect(MorpheusUser user)
    {
        Debug.Log($"Server CmdTeleportationEffect");
        RpcTeleportationEffect(user);
    }

    [ClientRpc]
    private void RpcTeleportationEffect(MorpheusUser user)
    {
        targetUser = user;
        if (user == MorpheusUser.LocalUser)
        {
            Debug.Log($"Local client RpcTeleportationEffect");
            StartCoroutine(TeleportationRoutine());
        }
        else
        {
            Debug.Log($"Client RpcTeleportationEffect");
            if (centerAlphaEffect != null)
            {
                StopCoroutine(centerAlphaEffect);
            }
            if (deepEffect != null)
            {
                StopCoroutine(deepEffect);
            }
            TeleportationEffectRoutine();
        }
    }

    Coroutine centerAlphaEffect;
    Coroutine deepEffect;

    private void TeleportationEffectRoutine()
    {
        isPlayingUserEffect = true;

        deepEffect = StartCoroutine(DeepController());
        centerAlphaEffect = StartCoroutine(CenterAlphaController());
        var dist = Vector3.Distance(transform.position, targetUser.transform.position);
        StartCoroutine(UserTeleportationEffectRoutine(1 - (2 / (dist + 2)), 1, userTeleportationEffectTime, dist));
    }

    const float multiplier = 2;

    private IEnumerator DeepController()
    {
        yield return DeepLerpRoutine(5f, 0.1f, 0.15f * multiplier);
        yield return DeepLerpRoutine(0.1f, 1.7f, 0.1f * multiplier);
        yield return DeepLerpRoutine(1.7f, 0.5f, 0.1f * multiplier);
        yield return DeepLerpRoutine(0.5f, 1.3f, 0.07f * multiplier);
        yield return DeepLerpRoutine(1.3f, 0.8f, 0.08f * multiplier);
        yield return DeepLerpRoutine(0.8f, 1.1f, 0.05f * multiplier);
        yield return DeepLerpRoutine(1.1f, 0.95f, 0.04f * multiplier);
        yield return DeepLerpRoutine(0.95f, 1f, 0.04f * multiplier);

        isPlayingUserEffect = false;
    }

    private IEnumerator CenterAlphaController()
    {
        yield return CenterAlphaLerpRoutine(0, 1, 0.1f * multiplier);
        yield return CenterAlphaLerpRoutine(1, 0, 0.93f * multiplier);
    }

    private IEnumerator CenterAlphaLerpRoutine(float startValue, float endValue, float time)
    {
        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            portalRenderer.material.SetFloat("_CenterAlpha", Mathf.Lerp(startValue, endValue, elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        portalRenderer.material.SetFloat("_CenterAlpha", endValue);
    }

    private IEnumerator DeepLerpRoutine(float startValue, float endValue, float time)
    {
        if (portalRenderer == null)
            Debug.LogError("portal renderer is null");

        float elapsedTime = 0;
        var scale = portalRenderer.transform.localScale;

        while (elapsedTime < time)
        {
            scale = portalRenderer.transform.localScale;
            portalRenderer.transform.localScale = new Vector3(Mathf.Lerp(startValue, endValue, elapsedTime / time), scale.y, scale.z);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        portalRenderer.transform.localScale = new Vector3(endValue, scale.y, scale.z);
    }

    MorpheusUser targetUser;

    private IEnumerator UserTeleportationEffectRoutine(float startValue, float endValue, float time, float dist)
    {
        SkinnedMeshRenderer renderer = targetUser.GetComponentInChildren<SkinnedMeshRenderer>();

        if (renderer == null)
            Debug.LogError("user renderer is null");

        var avatarTex = renderer.material.mainTexture;

        Debug.Log($"Avatar texture name - {avatarTex.name}, teleportation material - {targetUser.teleportationEffectMaterial.name}");

        renderer.material = targetUser.teleportationEffectMaterial;
        renderer.material.SetTexture("_MainTex", avatarTex);

        renderer.material.SetFloat("_Range", dist + 2);

        var portalPos = transform.position;
        portalPos.y = renderer.transform.position.y + portalRenderer.material.GetFloat("_Height");
        renderer.material.SetVector("_TeleportPos", portalPos);

        float elapsedTime = 0;

        while (elapsedTime < time)
        {
            renderer.material.SetFloat("_Strenght", Mathf.Lerp(startValue, endValue, elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        renderer.material.SetFloat("_Strenght", endValue);
    }

    private IEnumerator MainColorLerpRoutine(Color startValue, Color endValue, float time)
    {
        float elapsedTime = 0;
        portalRenderer.material.SetColor("_Color", startValue);

        while (elapsedTime < time)
        {
            portalRenderer.material.SetColor("_Color", Color.Lerp(portalRenderer.material.GetColor("_Color"), endValue, elapsedTime / time));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        portalRenderer.material.SetColor("_Color", endValue);
    }

    IEnumerator TeleportationRoutine()
    {
        var networkManager = FindObjectOfType<MorpheusNetworkManager>();

        if (networkManager == null)
        {
            Debug.LogWarning("Network manager is null");
        }
        else networkManager.loadingCam.gameObject.SetActive(true);

        var loadingManager = FindObjectOfType<LoadingManager>();

        if (loadingManager == null)
        {
            Debug.LogWarning("Loading Manager is null");
        }
        else loadingManager.loadingStatus.text = "0%";

        yield return new WaitForSeconds(userTeleportationEffectTime);

        var localUser = MorpheusUser.LocalUser;

        MorpheusNetworkManager.SendPlayerMessage msg = new MorpheusNetworkManager.SendPlayerMessage
        {
            player = localUser.gameObject,
            sendingZoneId = localUser.ZoneID,
            destinationZoneId = targetZoneId,
            destinationPortalName = targetPortalName
        };

        NetworkClient.Send(msg);
        Debug.Log($"Send player to {targetZoneId}:{targetPortalName}");
        yield return new WaitForSeconds(1);

        isTeleportUser = false;
    }

#endif
}