using System;
using System.IO;
using System.Security.Cryptography;

class Program
{
    static void Main()
    {
        // 루트 폴더의 경로를 지정하세요.
        string rootFolder = @"g:\@Example\AI\@Python_AI\yolov8\test\@datasets\train_data\Drugs\@latest_drugs_processing2";

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

                    // 파일 이름을 매칭하여 변경 (파일명 패딩 추가)
                    RenameFilesToMatch(labelsFolder, imagesFolder);

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

        Console.WriteLine("모든 작업 완료.");
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
}
