﻿using OpenCvSharp.CPlusPlus;
using System.Threading.Tasks;
using FaceRecLibrary.Utilities;
using FaceRecLibrary.Types;

namespace FaceRecLibrary
{
    public class CombinedClassifierFaceDetector
    {
        ClassifierList cList;

        bool useEyeDetection = true;
        bool enhanceImages = true;

        public CombinedClassifierFaceDetector()
        {
            cList = new ClassifierList();
        }

        public CombinedClassifierFaceDetector(ClassifierList clist)
        {
            this.cList = clist;
        }

        public CombinedClassifierFaceDetector(string configFile)
        {
            cList = Util.LoadXmlConfigFile(configFile);
        }

        public void AddClassifier(ClassifierInfo cInfo)
        {
            if(cInfo is FaceClassifier)
            {
                cList.FaceClassifiers.Add((FaceClassifier) cInfo);
            }
            else if (cInfo is EyeClassifier)
            {
                cList.EyeClassifiers.Add((EyeClassifier) cInfo);
            }
        }

        public int DetectFaces(ImageInfo imgInfo)
        {
            FaceClassifier[] faceClassifiers = cList.FaceClassifiers.ToArray();
            EyeClassifier[] eyeClassifier = cList.EyeClassifiers.ToArray();
            DetectionInfo existingInfo = imgInfo.DetectionInfo;
            DetectionInfo[] dInfo;

            if (existingInfo != null)
            {
                dInfo = new DetectionInfo[faceClassifiers.Length + 1];
                dInfo[faceClassifiers.Length] = existingInfo;
            }
            else
                dInfo = new DetectionInfo[faceClassifiers.Length];
            
            Parallel.For(0, faceClassifiers.Length, (i) =>
            {
                Rect[] detectionAreas = FaceDetect.RunDetection(imgInfo, faceClassifiers[i]);
                dInfo[i] = new DetectionInfo(Util.CvtRectToRectangle(detectionAreas), faceClassifiers[i].Confidence);
            });
            
            //Merge and prune detections
            DetectionInfo mergedDetections = Util.MergeDuplicates(dInfo);

            DetectionInfo finalResult = mergedDetections;
            if (useEyeDetection)
            {
                //Further pruning through eye detection
                finalResult = FaceDetect.DetectEyes(imgInfo, eyeClassifier, mergedDetections);
                
            }
            imgInfo.DetectionInfo = finalResult;
            return finalResult.Detections.Count;
        }
    }
}
