using System;
using System.IO;

class Program
{
    static void Main()
    {
        // 루트 폴더의 경로를 지정하세요.
        string rootFolder = @"g:\@Example\AI\@Python_AI\yolov8\test\@datasets\train_data\Drugs\@latest_drugs";

        // 처리할 하위 폴더 목록
        string[] subfolders = { "train", "test", "valid" };

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
                    // 라벨 파일 크기가 0인 경우 해당 라벨 파일과 이미지 파일 삭제
                    DeleteZeroSizeLabelAndCorrespondingImages(labelsFolder, imagesFolder);

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

    // 라벨 파일 크기가 0인 경우 해당 라벨 파일과 이미지 파일 삭제
    static void DeleteZeroSizeLabelAndCorrespondingImages(string labelsFolder, string imagesFolder)
    {
        // labels 폴더의 모든 파일을 가져옵니다.
        string[] labelFiles = Directory.GetFiles(labelsFolder);

        foreach (string labelFilePath in labelFiles)
        {
            FileInfo labelFileInfo = new FileInfo(labelFilePath);

            // 라벨 파일의 크기가 0인지 확인합니다.
            if (labelFileInfo.Length == 0)
            {
                // 라벨 파일을 삭제합니다.
                try
                {
                    File.Delete(labelFilePath);
                    Console.WriteLine($"라벨 파일 삭제됨: {labelFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"라벨 파일 삭제 중 오류 발생 {labelFilePath}: {ex.Message}");
                }

                // 파일 이름에서 확장자를 제거합니다.
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(labelFilePath);

                // images 폴더에서 동일한 이름의 파일을 찾습니다 (확장자 무시).
                string searchPattern = filenameWithoutExtension + ".*";
                string[] matchingImageFiles = Directory.GetFiles(imagesFolder, searchPattern);

                foreach (string imageFilePath in matchingImageFiles)
                {
                    try
                    {
                        // 이미지 파일을 삭제합니다.
                        File.Delete(imageFilePath);
                        Console.WriteLine($"이미지 파일 삭제됨: {imageFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"이미지 파일 삭제 중 오류 발생 {imageFilePath}: {ex.Message}");
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
}
