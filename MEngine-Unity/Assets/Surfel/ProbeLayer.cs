using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProbeLayer : MonoBehaviour
{
    public enum ProbeLayerChoose
    {
        Layer1 = 1 << 0,
        Layer2 = 1 << 1,
        Layer3 = 1 << 2,
        Layer4 = 1 << 3,
        Layer5 = 1 << 4,
        Layer6 = 1 << 5,
        Layer7 = 1 << 6,
        Layer8 = 1 << 7,
        Layer9 = 1 << 8,
        Layer10 = 1 << 9,
        Layer11 = 1 << 10,
        Layer12 = 1 << 11,
        Layer13 = 1 << 12,
        Layer14 = 1 << 13,
        Layer15 = 1 << 14,
        Layer16 = 1 << 15,
        Layer17 = 1 << 16,
        Layer18 = 1 << 17,
        Layer19 = 1 << 18,
        Layer20 = 1 << 19,
        Layer21 = 1 << 20,
        Layer22 = 1 << 21,
        Layer23 = 1 << 22,
        Layer24 = 1 << 23,
        Layer25 = 1 << 24,
        Layer26 = 1 << 25,
        Layer27 = 1 << 26,
        Layer28 = 1 << 27,
        Layer29 = 1 << 28,
        Layer30 = 1 << 29,
        Layer31 = 1 << 30,
        Layer32 = 1 << 31,
        Layer33 = 1 << 32
    };
    public ProbeLayerChoose targetLayer;
}
