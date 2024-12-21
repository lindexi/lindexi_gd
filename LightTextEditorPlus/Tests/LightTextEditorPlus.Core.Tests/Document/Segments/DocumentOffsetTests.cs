using LightTextEditorPlus.Core.Document.Segments;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document.Segments;

[TestClass]
public class DocumentOffsetTest
{
    [ContractTestCase]
    public void Equals()
    {
        "两个为默认的文档偏移，判断相等".Test(() =>
        {
            // Arrange
            DocumentOffset documentOffsetA = default;
            DocumentOffset documentOffsetB = default;

            // Action

            // Assert
            Assert.AreEqual(false, documentOffsetA > documentOffsetB);
            Assert.AreEqual(false, documentOffsetA < documentOffsetB);
            Assert.AreEqual(true, documentOffsetA >= documentOffsetB);
            Assert.AreEqual(true, documentOffsetA <= documentOffsetB);
            Assert.AreEqual(true, documentOffsetA == documentOffsetB);
            Assert.AreEqual(false, documentOffsetA != documentOffsetB);
        });

        "如果一个文档偏移为默认值，和另外一个不为默认值的文档偏移或整数判断，返回不相等".Test(() =>
        {
            // Arrange
            var offsetB = 10;
            DocumentOffset documentOffsetA = default;
            var documentOffsetB = new DocumentOffset(offsetB);

            // Action

            // Assert
            Assert.AreEqual(false, documentOffsetA > documentOffsetB);
            Assert.AreEqual(false, documentOffsetA > offsetB);

            Assert.AreEqual(false, documentOffsetA >= documentOffsetB);
            Assert.AreEqual(false, documentOffsetA >= offsetB);

            Assert.AreEqual(true , documentOffsetA < documentOffsetB);
            Assert.AreEqual(true, documentOffsetA < offsetB);

            Assert.AreEqual(true, documentOffsetA <= documentOffsetB);
            Assert.AreEqual(true, documentOffsetA <= offsetB);

            Assert.AreEqual(false, documentOffsetA == offsetB);
            Assert.AreEqual(false, documentOffsetA == documentOffsetB);
            Assert.AreEqual(true, documentOffsetA != offsetB);
            Assert.AreEqual(true, documentOffsetA != documentOffsetB);
        });

        "如果文档偏移a的偏移值大于文档偏移b的值，可以用偏移量判断大小".Test(() =>
        {
            // Arrange
            var offsetA = 100;
            var offsetB = 10;

            var documentOffsetA = new DocumentOffset(offsetA);
            var documentOffsetB = new DocumentOffset(offsetB);

            // Action

            // Assert
            Assert.AreEqual(true, documentOffsetA > documentOffsetB);
            Assert.AreEqual(true, documentOffsetA > offsetB);
            Assert.AreEqual(true, offsetA > documentOffsetB);

            Assert.AreEqual(true, documentOffsetA >= documentOffsetB);
            Assert.AreEqual(true, documentOffsetA >= offsetB);
            Assert.AreEqual(true, offsetA >= documentOffsetB);

            Assert.AreEqual(false, documentOffsetA < documentOffsetB);
            Assert.AreEqual(false, documentOffsetA < offsetB);
            Assert.AreEqual(false, offsetA < documentOffsetB);

            Assert.AreEqual(false, documentOffsetA <= documentOffsetB);
            Assert.AreEqual(false, documentOffsetA <= offsetB);
            Assert.AreEqual(false, offsetA <= documentOffsetB);
        });

        "两个文档偏移的偏移值不相等，判断两个文档偏移的值，返回不相等".Test(() =>
        {
            var documentOffset1 = new DocumentOffset(10);
            var documentOffset2 = new DocumentOffset(11);

            // Assert
            Assert.AreEqual(false, documentOffset1 == documentOffset2);
            Assert.AreEqual(false, documentOffset1.Equals(documentOffset2));
            Assert.AreEqual(true, documentOffset1 != documentOffset2);
        });

        "一个文档偏移和偏移值相同的整数判断，可以返回相等".Test(() =>
        {
            // Arrange
            var offset = 10;
            var documentOffset = new DocumentOffset(offset);

            // Action

            // Assert
            Assert.AreEqual(true, offset == documentOffset);
            Assert.AreEqual(true, documentOffset == offset);
            Assert.AreEqual(true, documentOffset.Equals(offset));
            Assert.AreEqual(true, documentOffset >= offset);
            Assert.AreEqual(true, documentOffset <= offset);
            Assert.AreEqual(false, documentOffset != offset);
        });

        "两个文档偏移的偏移值相同，可以返回相等".Test(() =>
        {
            // Arrange
            var offset = 10;
            var documentOffset1 = new DocumentOffset(offset);
            var documentOffset2 = new DocumentOffset(offset);

            // Action

            // Assert
            Assert.AreEqual(true, documentOffset1 == documentOffset2);
            Assert.AreEqual(true, documentOffset1.Equals(documentOffset2));
            Assert.AreEqual(true, documentOffset1 >= documentOffset2);
            Assert.AreEqual(true, documentOffset1 <= documentOffset2);
            Assert.AreEqual(false, documentOffset1 != documentOffset2);
        });
    }
}