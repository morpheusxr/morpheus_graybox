using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneInfo : MonoBehaviour
{
    public Material skyboxMaterial;
    public Transform defaultStartPosition;
    public List<PortalInfo> portalsList;

    public Dictionary<string, PortalInfo> portals = new Dictionary<string, PortalInfo>();

    private void Awake()
    {
        foreach (var portal in portalsList)
        {
            portals[portal.name] = portal;
        }
    }
}