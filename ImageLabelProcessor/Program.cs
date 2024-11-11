using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        // 루트 폴더의 경로를 지정하세요.
        string rootFolder = @"g:\@Example\AI\@Python_AI\yolov8\test\@datasets\train_data\Nude\@@@Dataset\nude\total_nude_content";

        // 처리할 하위 폴더 목록
        string[] subfolders = { "train", "test", "valid" };

        // 삭제할지 분류할지 선택합니다. true이면 삭제, false이면 분류(복사)
        bool isDelete = true;
            
        // 분류(복사)할 경우, 파일을 복사할 대상 디렉토리 경로를 지정하세요.
        string classificationFolder = @"g:\@Example\AI\@Python_AI\yolov8\test\@datasets\train_data\Drugs\@latest_drugs - 복사본\classfications";
        
        foreach (var subfolder in subfolders)
        {
            string subfolderPath = Path.Combine(rootFolder, subfolder);

            // 하위 폴더가 존재하는지 확인합니다.
            if (Directory.Exists(subfolderPath))
            {
                Console.WriteLine($"처리 중: {subfolderPath}");

                // images와 labels 폴더의 경로를 설정합니다.
                string imagesFolder = Path.Combine(subfolderPath, "images");
                string labelsFolder = Path.Combine(subfolderPath, "labels");

                // images와 labels 폴더가 존재하는지 확인합니다.
                if (Directory.Exists(imagesFolder) && Directory.Exists(labelsFolder))
                {
                    // 라벨 파일 크기가 0인 경우 해당 라벨 파일과 이미지 파일 삭제 또는 분류
                    DeleteOrClassifyZeroSizeLabelAndCorrespondingImages(labelsFolder, imagesFolder, isDelete, classificationFolder);

                    //중복 이미지 및 라벨 제거
                    RemoveDuplicateImagesAndLabels(labelsFolder, imagesFolder);
                }
                else
                {
                    Console.WriteLine($"images 또는 labels 폴더가 없습니다: {subfolderPath}");
                }
            }
            else
            {
                Console.WriteLine($"폴더가 존재하지 않습니다: {subfolderPath}");
            }
        }
   
        AdjustDatasetSplits(rootFolder);

        foreach (var subfolder in subfolders)
        {
            string subfolderPath = Path.Combine(rootFolder, subfolder);

            // 하위 폴더가 존재하는지 확인합니다.
            if (Directory.Exists(subfolderPath))
            {
                Console.WriteLine($"처리 중: {subfolderPath}");

                // images와 labels 폴더의 경로를 설정합니다.
                string imagesFolder = Path.Combine(subfolderPath, "images");
                string labelsFolder = Path.Combine(subfolderPath, "labels");

                // images와 labels 폴더가 존재하는지 확인합니다.
                if (Directory.Exists(imagesFolder) && Directory.Exists(labelsFolder))
                {          
                    // 파일 이름을 매칭하여 변경 (파일명 패딩 추가)
                    RenameFilesToMatch(labelsFolder, imagesFolder);
                }
                else
                {
                    Console.WriteLine($"images 또는 labels 폴더가 없습니다: {subfolderPath}");
                }
            }
            else
            {
                Console.WriteLine($"폴더가 존재하지 않습니다: {subfolderPath}");
            }
        }

        Console.WriteLine("모든 작업 완료.");
    }

    // 데이터셋 비율을 조정하는 함수
    static void AdjustDatasetSplits(string rootFolder)
    {
        // 분할 비율 설정
        double trainRatio = 0.7;
        double testRatio = 0.15;
        double validRatio = 0.15;

        // 각 데이터셋 폴더 경로 설정
        string trainImagesFolder = Path.Combine(rootFolder, "train", "images");
        string trainLabelsFolder = Path.Combine(rootFolder, "train", "labels");

        string testImagesFolder = Path.Combine(rootFolder, "test", "images");
        string testLabelsFolder = Path.Combine(rootFolder, "test", "labels");

        string validImagesFolder = Path.Combine(rootFolder, "valid", "images");
        string validLabelsFolder = Path.Combine(rootFolder, "valid", "labels");

        // 이미지 파일 확장자 설정 (필요 시 수정)
        string[] imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tif", ".tiff" };
        string[] labelExtensions = new[] { ".txt", ".xml", ".json" }; // 사용 중인 라벨 파일 확장자에 맞게 수정

        // 각 폴더의 이미지 파일 목록 가져오기
        var trainImageFiles = GetFilesByExtensions(trainImagesFolder, imageExtensions);
        var testImageFiles = GetFilesByExtensions(testImagesFolder, imageExtensions);
        var validImageFiles = GetFilesByExtensions(validImagesFolder, imageExtensions);

        // 전체 이미지 수 계산
        int totalImages = trainImageFiles.Count + testImageFiles.Count + validImageFiles.Count;

        // 각 데이터셋의 목표 이미지 수 계산
        int desiredTrainCount = (int)(totalImages * trainRatio);
        int desiredTestCount = (int)(totalImages * testRatio);
        int desiredValidCount = totalImages - desiredTrainCount - desiredTestCount; // 나머지는 valid에 할당

        // 현재 각 데이터셋의 이미지 수
        int currentTrainCount = trainImageFiles.Count;
        int currentTestCount = testImageFiles.Count;
        int currentValidCount = validImageFiles.Count;

        Console.WriteLine("\n데이터셋 비율 조정:");
        Console.WriteLine($"Total Images: {totalImages}");
        Console.WriteLine($"Train Images: {currentTrainCount} -> {desiredTrainCount}");
        Console.WriteLine($"Test Images: {currentTestCount} -> {desiredTestCount}");
        Console.WriteLine($"Valid Images: {currentValidCount} -> {desiredValidCount}");

        // 이동해야 할 이미지 수 계산
        int needToMoveFromTrainToTest = desiredTestCount - currentTestCount;
        int needToMoveFromTrainToValid = desiredValidCount - currentValidCount;

        // 필요한 경우 이미지 이동
        if (needToMoveFromTrainToTest > 0)
        {
            MoveImagesAndLabels(trainImagesFolder, trainLabelsFolder, testImagesFolder, testLabelsFolder, needToMoveFromTrainToTest, imageExtensions, labelExtensions);
        }
        else if (needToMoveFromTrainToTest < 0)
        {
            // test에서 train으로 이동
            MoveImagesAndLabels(testImagesFolder, testLabelsFolder, trainImagesFolder, trainLabelsFolder, -needToMoveFromTrainToTest, imageExtensions, labelExtensions);
        }

        if (needToMoveFromTrainToValid > 0)
        {
            MoveImagesAndLabels(trainImagesFolder, trainLabelsFolder, validImagesFolder, validLabelsFolder, needToMoveFromTrainToValid, imageExtensions, labelExtensions);
        }
        else if (needToMoveFromTrainToValid < 0)
        {
            // valid에서 train으로 이동
            MoveImagesAndLabels(validImagesFolder, validLabelsFolder, trainImagesFolder, trainLabelsFolder, -needToMoveFromTrainToValid, imageExtensions, labelExtensions);
        }

        Console.WriteLine("\n데이터셋 조정 완료.");

        // 조정 후 각 데이터셋의 이미지 수 출력
        trainImageFiles = GetFilesByExtensions(trainImagesFolder, imageExtensions);
        testImageFiles = GetFilesByExtensions(testImagesFolder, imageExtensions);
        validImageFiles = GetFilesByExtensions(validImagesFolder, imageExtensions);

        Console.WriteLine("\n조정 후 데이터셋 상태:");
        Console.WriteLine($"Train Images: {trainImageFiles.Count}");
        Console.WriteLine($"Test Images: {testImageFiles.Count}");
        Console.WriteLine($"Valid Images: {validImageFiles.Count}");
    }

    static List<string> GetFilesByExtensions(string path, string[] extensions)
    {
        if (!Directory.Exists(path))
        {
            return new List<string>();
        }

        return Directory.GetFiles(path)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
            .ToList();
    }

    static void MoveImagesAndLabels(string sourceImagesFolder, string sourceLabelsFolder, string targetImagesFolder, string targetLabelsFolder, int numberOfFilesToMove, string[] imageExtensions, string[] labelExtensions)
    {
        var imageFiles = GetFilesByExtensions(sourceImagesFolder, imageExtensions);

        if (imageFiles.Count == 0)
        {
            Console.WriteLine($"소스 폴더에 이미지 파일이 없습니다: {sourceImagesFolder}");
            return;
        }

        // 랜덤하게 파일 선택
        var random = new Random();
        var filesToMove = imageFiles.OrderBy(x => random.Next()).Take(numberOfFilesToMove).ToList();

        // 대상 폴더가 없으면 생성
        Directory.CreateDirectory(targetImagesFolder);
        Directory.CreateDirectory(targetLabelsFolder);

        foreach (var imageFilePath in filesToMove)
        {
            string fileName = Path.GetFileName(imageFilePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);

            // 이미지 파일 이동
            string destImagePath = Path.Combine(targetImagesFolder, fileName);
            try
            {
                File.Move(imageFilePath, destImagePath);
                Console.WriteLine($"이미지 파일 이동: {imageFilePath} -> {destImagePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 파일 이동 중 오류 발생: {ex.Message}");
                continue;
            }

            // 라벨 파일 이동
            bool labelFound = false;
            foreach (var ext in labelExtensions)
            {
                string labelFileName = fileNameWithoutExtension + ext;
                string sourceLabelPath = Path.Combine(sourceLabelsFolder, labelFileName);
                if (File.Exists(sourceLabelPath))
                {
                    string destLabelPath = Path.Combine(targetLabelsFolder, labelFileName);
                    try
                    {
                        File.Move(sourceLabelPath, destLabelPath);
                        Console.WriteLine($"라벨 파일 이동: {sourceLabelPath} -> {destLabelPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"라벨 파일 이동 중 오류 발생: {ex.Message}");
                    }
                    labelFound = true;
                    break;
                }
            }

            if (!labelFound)
            {
                Console.WriteLine($"라벨 파일을 찾을 수 없습니다: {fileNameWithoutExtension}");
            }
        }
    }
    // 라벨 파일 크기가 0인 경우 해당 라벨 파일과 이미지 파일 삭제 또는 분류
    static void DeleteOrClassifyZeroSizeLabelAndCorrespondingImages(string labelsFolder, string imagesFolder, bool isDelete, string classificationFolder)
    {
        // labels 폴더의 모든 파일을 가져옵니다.
        string[] labelFiles = Directory.GetFiles(labelsFolder);

        foreach (string labelFilePath in labelFiles)
        {
            FileInfo labelFileInfo = new FileInfo(labelFilePath);

            // 라벨 파일의 크기가 0인지 확인합니다.
            if (labelFileInfo.Length == 0)
            {
                // 파일 이름에서 확장자를 제거합니다.
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(labelFilePath);

                // images 폴더에서 동일한 이름의 파일을 찾습니다 (확장자 무시).
                string searchPattern = filenameWithoutExtension + ".*";
                string[] matchingImageFiles = Directory.GetFiles(imagesFolder, searchPattern);

                if (isDelete)
                {
                    // 라벨 파일과 이미지 파일을 삭제합니다.
                    try
                    {
                        File.Delete(labelFilePath);
                        Console.WriteLine($"라벨 파일 삭제됨: {labelFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"라벨 파일 삭제 중 오류 발생 {labelFilePath}: {ex.Message}");
                    }

                    foreach (string imageFilePath in matchingImageFiles)
                    {
                        try
                        {
                            File.Delete(imageFilePath);
                            Console.WriteLine($"이미지 파일 삭제됨: {imageFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"이미지 파일 삭제 중 오류 발생 {imageFilePath}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // 라벨 파일과 이미지 파일을 지정된 디렉토리로 복사합니다.
                    foreach (string imageFilePath in matchingImageFiles)
                    {
                        try
                        {
                            string destImagePath = Path.Combine(classificationFolder, "images", Path.GetFileName(imageFilePath));
                            Directory.CreateDirectory(Path.GetDirectoryName(destImagePath));
                            File.Copy(imageFilePath, destImagePath, true);
                            Console.WriteLine($"이미지 파일 분류됨: {imageFilePath} -> {destImagePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"이미지 파일 복사 중 오류 발생 {imageFilePath}: {ex.Message}");
                        }
                    }

                    try
                    {
                        string destLabelPath = Path.Combine(classificationFolder, "labels", Path.GetFileName(labelFilePath));
                        Directory.CreateDirectory(Path.GetDirectoryName(destLabelPath));
                        File.Copy(labelFilePath, destLabelPath, true);
                        Console.WriteLine($"라벨 파일 분류됨: {labelFilePath} -> {destLabelPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"라벨 파일 복사 중 오류 발생 {labelFilePath}: {ex.Message}");
                    }

                    // 원본 파일을 삭제합니다.
                    try
                    {
                        File.Delete(labelFilePath);
                        Console.WriteLine($"원본 라벨 파일 삭제됨: {labelFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"원본 라벨 파일 삭제 중 오류 발생 {labelFilePath}: {ex.Message}");
                    }

                    foreach (string imageFilePath in matchingImageFiles)
                    {
                        try
                        {
                            File.Delete(imageFilePath);
                            Console.WriteLine($"원본 이미지 파일 삭제됨: {imageFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"원본 이미지 파일 삭제 중 오류 발생 {imageFilePath}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    // 이미지 및 라벨 파일 이름을 5자리 숫자로 변경 (예: 00001.jpg, 00001.txt)
    static void RenameFilesToMatch(string labelsFolder, string imagesFolder)
    {
        // images 폴더의 모든 이미지 파일을 가져옵니다.
        var imageFiles = Directory.GetFiles(imagesFolder);
        int index = 1;

        foreach (var imageFilePath in imageFiles)
        {
            string imageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);
            string imageExtension = Path.GetExtension(imageFilePath);

            // labels 폴더에서 동일한 이름의 라벨 파일을 찾습니다 (확장자 무시).
            var matchingLabelFiles = Directory.GetFiles(labelsFolder, imageFileNameWithoutExtension + ".*");

            if (matchingLabelFiles.Length > 0)
            {
                string labelFilePath = matchingLabelFiles[0]; // 매칭되는 첫 번째 라벨 파일 사용
                string labelExtension = Path.GetExtension(labelFilePath);

                // 새로운 파일명 생성 (예: 00001.jpg, 00001.txt)
                string newFileName = index.ToString("D5"); // D5는 5자리로 패딩

                string newImageFilePath = Path.Combine(imagesFolder, newFileName + imageExtension);
                string newLabelFilePath = Path.Combine(labelsFolder, newFileName + labelExtension);

                try
                {
                    // 이미지 파일명 변경
                    File.Move(imageFilePath, newImageFilePath);
                    Console.WriteLine($"이미지 파일명 변경: {imageFilePath} -> {newImageFilePath}");

                    // 라벨 파일명 변경
                    File.Move(labelFilePath, newLabelFilePath);
                    Console.WriteLine($"라벨 파일명 변경: {labelFilePath} -> {newLabelFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"파일명 변경 중 오류 발생: {ex.Message}");
                }

                index++;
            }
            else
            {
                Console.WriteLine($"매칭되는 라벨 파일이 없습니다: {imageFilePath}");
            }
        }
    }
    // 이미지와 라벨 파일의 중복을 찾아 제거하는 함수
    static void RemoveDuplicateImagesAndLabels(string labelsFolder, string imagesFolder)
    {
        // 이미지 파일의 해시값을 저장할 딕셔너리 (해시값, 파일 경로)
        Dictionary<string, string> hashDictionary = new Dictionary<string, string>();

        // 삭제할 이미지 및 라벨 파일 경로를 저장할 리스트
        List<string> duplicateImageFiles = new List<string>();
        List<string> duplicateLabelFiles = new List<string>();

        // 이미지 폴더의 모든 파일을 가져옵니다.
        string[] imageFiles = Directory.GetFiles(imagesFolder);

        foreach (string imageFilePath in imageFiles)
        {
            try
            {
                // 이미지 파일의 MD5 해시값을 계산합니다.
                string fileHash = GetFileHash(imageFilePath);

                if (hashDictionary.ContainsKey(fileHash))
                {
                    // 중복된 파일로 판단하여 리스트에 추가합니다.
                    duplicateImageFiles.Add(imageFilePath);

                    Console.WriteLine($"중복 이미지 발견: {imageFilePath}");

                    // 해당 이미지와 매칭되는 라벨 파일을 찾습니다.
                    string imageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(imageFilePath);
                    string[] matchingLabelFiles = Directory.GetFiles(labelsFolder, imageFileNameWithoutExtension + ".*");

                    foreach (string labelFilePath in matchingLabelFiles)
                    {
                        duplicateLabelFiles.Add(labelFilePath);
                    }
                }
                else
                {
                    // 해시값을 딕셔너리에 추가합니다.
                    hashDictionary.Add(fileHash, imageFilePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"해시 계산 중 오류 발생 {imageFilePath}: {ex.Message}");
            }
        }

        // 중복된 이미지 파일을 삭제합니다.
        foreach (string duplicateImage in duplicateImageFiles)
        {
            try
            {
                File.Delete(duplicateImage);
                Console.WriteLine($"중복 이미지 삭제됨: {duplicateImage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"이미지 파일 삭제 중 오류 발생 {duplicateImage}: {ex.Message}");
            }
        }

        // 중복된 라벨 파일을 삭제합니다.
        foreach (string duplicateLabel in duplicateLabelFiles)
        {
            try
            {
                File.Delete(duplicateLabel);
                Console.WriteLine($"중복 라벨 삭제됨: {duplicateLabel}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"라벨 파일 삭제 중 오류 발생 {duplicateLabel}: {ex.Message}");
            }
        }
    }

    // 파일의 MD5 해시값을 계산하는 함수
    static string GetFileHash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hashBytes = md5.ComputeHash(stream);
                // 해시값을 문자열로 변환합니다.
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }


    static List<string> FindInstanceSegments(string rootFolder)
    {
        // List to store image filenames with bounding box annotations
        List<string> boundingBoxImageFilenames = new List<string>();

        // Set the labels folder path
        string labelsFolderPath = Path.Combine(rootFolder, "labels");

        if (Directory.Exists(labelsFolderPath))
        {
            // Loop over all annotation files in the labels folder
            foreach (var annotationFile in Directory.GetFiles(labelsFolderPath, "*.txt"))
            {
                var lines = File.ReadAllLines(annotationFile);

                // Check each line in the annotation file
                foreach (var line in lines)
                {
                    // Split the line to get the elements
                    var elements = line.Trim().Split();

                    // YOLOv8 object detection (bounding box) should have 5 elements: class_id, x_center, y_center, width, height
                    if (elements.Length > 5)
                    {
                        string imageFilename = Path.GetFileNameWithoutExtension(annotationFile) + ".jpg"; // Assuming image is .jpg
                        string imagePath = Path.Combine(rootFolder, "images", imageFilename);

                        if (File.Exists(imagePath))
                        {
                            boundingBoxImageFilenames.Add(imagePath);
                        }
                        break; // Stop further checks for this file as it's already added
                    }                
                }
            }
        }
        else
        {
            Console.WriteLine($"labels 폴더가 존재하지 않습니다: {labelsFolderPath}");
        }

        return boundingBoxImageFilenames;
    }
    static List<string> FindBoundingBoxes(string rootFolder)
    {
        // List to store image filenames with bounding box annotations
        List<string> boundingBoxImageFilenames = new List<string>();

        // Set the labels folder path
        string labelsFolderPath = Path.Combine(rootFolder, "labels");

        if (Directory.Exists(labelsFolderPath))
        {
            // Loop over all annotation files in the labels folder
            foreach (var annotationFile in Directory.GetFiles(labelsFolderPath, "*.txt"))
            {
                var lines = File.ReadAllLines(annotationFile);

                // Check each line in the annotation file
                foreach (var line in lines)
                {
                    // Split the line to get the elements
                    var elements = line.Trim().Split();

                    // YOLOv8 object detection (bounding box) should have 5 elements: class_id, x_center, y_center, width, height
                    if (elements.Length == 5)
                    {
                        string imageFilename = Path.GetFileNameWithoutExtension(annotationFile) + ".jpg"; // Assuming image is .jpg
                        string imagePath = Path.Combine(rootFolder, "images", imageFilename);

                        if (File.Exists(imagePath))
                        {
                            boundingBoxImageFilenames.Add(imagePath);
                        }
                        break; // Stop further checks for this file as it's already added
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"labels 폴더가 존재하지 않습니다: {labelsFolderPath}");
        }

        return boundingBoxImageFilenames;
    }
}
