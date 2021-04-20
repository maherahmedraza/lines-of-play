﻿/*
Copyright 2020 Google LLC

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    https://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class Paintable : MonoBehaviour
{

    public GameObject Brush;
    public float BrushSize = 0.01f;
    public RenderTexture RTexture;
    public Transform parent;
    public List<GameObject>  cubesList;
    public GameObject prefabs;
    public MainController mainController;
    private Vector3 startPos;
    private Vector3 endPos;
    GameObject prevBrushPoint;
    private List<Domino> holdDominos = new List<Domino>();
    public UndoRedoManager _undoRedoManager;

    private void OnDisable()
    {
        DeletePrefabs();
    }


    // Use this for initialization
    void Start()
    {
        if (mainController == null)
        {
            mainController = FindObjectOfType<MainController>();
        }
        if (_undoRedoManager == null)
        {
            _undoRedoManager = FindObjectOfType<UndoRedoManager>();
        }
    }

    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPointerOverUIObject())
        {
            return;
        }

        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
        {

            var Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(Ray, out hit))
            {
                startPos = hit.point;

            }
        }
        else if (((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved) || Input.GetMouseButton(0)))
        {
            var Ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(Ray, out hit))
            {
                if (hit.transform.CompareTag("DetectedPlane"))
                {
                    //instanciate a brush
                    if (Vector3.Distance(startPos, hit.point) > 0.10f)
                    {
                        
                        Vector3 right = Vector3.right;
                        if (hit.normal != Vector3.up)
                        {
                            right = Vector3.Cross(hit.normal, Vector3.up);
                        }
                        Vector3 forward = Vector3.Cross(hit.normal, right);
                        Quaternion orientation = Quaternion.identity;
                        orientation.SetLookRotation(forward, hit.normal);
                        var rotation = Quaternion.Slerp(orientation, transform.rotation, 0.75f);
                        var position = hit.point + (hit.normal * 0.01f);
                        var Distance = hit.distance;
                        
                        //var go = Instantiate(Brush, hit.point + Vector3.up * 0.1f, Quaternion.identity, parent);
                        var go = Instantiate(Brush, hit.point + Vector3.up * 0.1f, hit.transform.rotation, parent);
                        //var go = Instantiate(Brush, hit.point, Quaternion.identity, parent); //current
                        //var go = Instantiate(Brush, position, rotation, parent);
                       
                        
                        //go.transform.localScale = Vector3.one * BrushSize;
                        if (prevBrushPoint != null)
                        {
                            go.transform.LookAt(prevBrushPoint.transform);
                        }
                        cubesList.Add(go);
                        startPos = hit.point;
                        prevBrushPoint = go;
                    }
                }

            }
            endPos = hit.point;
        }
        else if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) || Input.GetMouseButtonUp(0))
        {
            if (Vector3.Distance(endPos, startPos) < 0.1)
            {
               // DeletePrefabs();
            }

            SpwanPrefabs();
            DeletePrefabs();
        }

    }

    public void SpwanPrefabs()
    {
        GameObject prevDomino=null;
        GameObject currDomino = null;
        holdDominos.Clear();
        for (int i = 0; i < cubesList.Count; i++)
        {
            currDomino = Instantiate(prefabs, cubesList[i].transform.position, Quaternion.identity);
            currDomino.GetComponent<SwitchOnRandomDomino>().colorID = PlayerPrefs.GetInt("isRandomColoring") == 1 ? Random.Range(1, 7) : PlayerPrefs.GetInt("ColorID");
            
            if (PlayerPrefs.GetInt("isStickyMode") == 0)
            {
                currDomino.GetComponent<SwitchOnRandomDomino>().Dominos[0].GetComponent<Rigidbody>().useGravity = true;
                print(currDomino.GetComponent<SwitchOnRandomDomino>().Dominos[0].name);
            }
            else if (PlayerPrefs.GetInt("isStickyMode") == 1)
            {
                currDomino.transform.GetChild(0).GetComponent<Rigidbody>().useGravity = false;
                currDomino.transform.GetChild(0).GetComponent<Rigidbody>().isKinematic = true;
                print(currDomino.transform.GetChild(0).name);
            }
            
            
            mainController.AddDomino(currDomino);
            if (prevDomino == null)
            {
                prevDomino = currDomino;
            }
            else
            {
                prevDomino.transform.LookAt(currDomino.transform);        
               
            }
            prevDomino = currDomino;

            Domino dominoTemp = new Domino();
            dominoTemp._dominoObj = prevDomino;
            dominoTemp._dominoPosition = prevDomino.transform.position;
            dominoTemp._dominoRotation = prevDomino.transform.rotation;
            dominoTemp._dominoScale = prevDomino.transform.localScale;
            holdDominos.Add(dominoTemp);
        }

        if (currDomino != null)
        {
            currDomino.transform.rotation = mainController.dominos[mainController.dominos.Count - 2].transform.rotation;
        }

        _undoRedoManager.LoadData(TransactionData.States.spawned, holdDominos);
        DeletePrefabs();
    }

    public void DeletePrefabs()
    {
        int i = cubesList.Count -1;

        while (i >= 0)
        {
            Destroy(cubesList[i]);
            cubesList.RemoveAt(i);

            i--;
        }
    }

}