using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System;
using SimpleJSON;

public class OnCollisionEvent : MonoBehaviour
{
    public static string server = "";
    public static string serverPath = server + "/Detection/ALL/CNN/API/Inference";

    private string username = "";
    private string password = "";

    public string image;
    public string imageName;

    float hitWait = 1;
    float hitClock = 0;
    bool hitOk = true;

    public Collider current = null;
    public Collider culprit = null;

    string[] materials = {
            "Im006_1",
            "Im020_1",
            "Im024_1",
            "Im026_1",
            "Im028_1",
            "Im031_1",
            "Im035_0",
            "Im041_0",
            "Im047_0",
            "Im053_1",
            "Im057_1",
            "Im060_1",
            "Im063_1",
            "Im069_0",
            "Im074_0",
            "Im088_0",
            "Im095_0",
            "Im099_0",
            "Im101_0",
            "Im106_0"
    };

    void resetBlocks()
    {
        foreach (string i in materials)
        {
            GameObject dataCube = GameObject.Find(i);
            dataCube.GetComponent<MeshRenderer>().material.color = Color.white;
        }
        current = null;
    }

    IEnumerator OnCollisionEnter(Collision collision)
    {
        if (!hitOk)
            yield break;

        ContactPoint contact = collision.contacts[0];

        culprit = contact.otherCollider;
        current = contact.thisCollider;
        print("Collision With " + current.name + " (Tag: " + current.tag + ") From " + culprit.name);


        if (current.name == "reset")
        {
            resetBlocks();
            yield break;
        }

        if (culprit.name == "Beamer")
        {
            print("Collision With " + current.name + " (Tag: " + current.tag + ") From " + culprit.name);

            imageName = current.name + ".jpg";
            image = Path.Combine(Application.streamingAssetsPath, imageName);

            List<IMultipartFormSection> form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", File.ReadAllBytes(image), imageName, "image/jpeg")
            };

            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
            byte[] body = new byte[formSections.Length + terminate.Length];

            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

            string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));

            UnityWebRequest wr = new UnityWebRequest(serverPath, "POST");

            string base64 = Convert.ToBase64String(
                Encoding.GetEncoding("UTF-8").GetBytes(username + ":" + password)
            );

            wr.SetRequestHeader("Authorization", "Basic " + base64);

            UploadHandler uploader = new UploadHandlerRaw(body);
            uploader.contentType = contentType;

            wr.uploadHandler = uploader;
            wr.downloadHandler = new DownloadHandlerBuffer();

            yield return wr.SendWebRequest();

            if (wr.isNetworkError || wr.isHttpError)
            {
                print(wr.error);
                hitClock = 0;
            }
            else
            {
                string json = wr.downloadHandler.text;
                JSONNode jsonData = JSON.Parse(System.Text.Encoding.UTF8.GetString(wr.downloadHandler.data));

                if (jsonData["Diagnosis"] == "Negative")
                {
                    print("Negative Classification");
                    if (imageName.Contains("_0"))
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.green;
                    }
                    else
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    }
                }
                else if (jsonData["Diagnosis"] == "Positive")
                {
                    print("Postive Classification");
                    if (imageName.Contains("_0"))
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    }
                    else
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.red;
                    }
                }
                hitClock = 0;
            }

        }
    }

    void OnCollisionStay(Collision collision)
    {
    }

    void OnCollisionExit(Collision collision)
    {
    }

    void Update()
    {
        hitClock += Time.deltaTime;
        if (hitClock > hitWait)
            hitOk = true;
    }
}