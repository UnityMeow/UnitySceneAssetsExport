﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
public class SearchText : ScriptableWizard
{
    public string folderPath;
    public string extent = "cginc";
    public string targetText;
    [MenuItem("MPipeline/Search Text")]
    private static void CreateWizard()
    {
        DisplayWizard<SearchText>("Search", "Close", "Print");
    }

    private void IterateFile(string folderPath)
    {
        foreach (string file in Directory.EnumerateFiles(folderPath, "*." + extent))
        {
            string contents = File.ReadAllText(file);
            if (contents.Contains(targetText))
                Debug.Log(file);
        }
        List<string> paths = new List<string>();
        foreach (string folder in Directory.EnumerateDirectories(folderPath))
        {
            string s = folder;
            paths.Add(s);
        }
        foreach (var i in paths)
        {
            IterateFile(i);
        }
    }
    private void OnWizardCreate()
    {

    }
    private void OnWizardOtherButton()
    {
        IterateFile(folderPath);
    }
}

public class TransformText : ScriptableWizard
{
    public string folderPath;
    public string extent = "cginc";
    public string targetText;
    public string exchangeText;
    [MenuItem("MPipeline/Transform Text")]
    private static void CreateWizard()
    {
        DisplayWizard<TransformText>("Search", "Close", "Print");
    }

    private List<string> IterateFile(string folderPath)
    {
        List<string> allPath = new List<string>(100);
        foreach (string file in Directory.EnumerateFiles(folderPath, "*." + extent))
        {
            allPath.Add(file);

        }
        List<string> paths = new List<string>();
        foreach (string folder in Directory.EnumerateDirectories(folderPath))
        {
            string s = folder;
            paths.Add(s);
        }
        foreach (var i in paths)
        {
            IterateFile(i);
        }
        return allPath;
    }
    private void OnWizardCreate()
    {

    }
    private void OnWizardOtherButton()
    {
        List<string> strs = IterateFile(folderPath);
        List<int> indices = new List<int>(100);
        string newStr = new string(' ', 1000);
        foreach (var str in strs)
        {
            
        }
    }
}