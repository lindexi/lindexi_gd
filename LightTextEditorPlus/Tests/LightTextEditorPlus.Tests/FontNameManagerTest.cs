using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class FontNameManagerTest
{
    [UIContractTestCase]
    public void RegisterFontFallback()
    {
        "���ı�����ע������ع������Գɹ�ע��".Test(() =>
        {
            TextEditor.StaticConfiguration.FontNameManager.RegisterFontFallback(GetDefaultFallback());
            TextEditor.StaticConfiguration.FontNameManager.RegisterFuzzyFontFallback(GetFuzzyFallback());
            // û�����쳣���ǳɹ�
        });

        "��������Ҳ����ع������ᴥ������ع�ʧ���¼�".Test(() =>
        {
            const string fontName = "һ�������ڵ�����xxxxasdasd";
            var count = 0;

            TextEditor.StaticConfiguration.FontNameManager.FontFallbackFailed += (sender, args) =>
            {
                if (args.FontName.Equals(fontName, StringComparison.Ordinal))
                {
                    count++;
                }
            };

            var (mainWindow, textEditor) = TestFramework.CreateTextEditorInNewWindow();
            var runPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();
            var styleRunProperty = runPropertyCreator.BuildNewProperty(config =>
            {
                var property = (RunProperty) config;
                property.FontName = new FontName(fontName);
            }, runPropertyCreator.GetDefaultRunProperty()).AsRunProperty();

            styleRunProperty.GetGlyphTypeface();

            Assert.AreEqual(true, count > 0);
        });
    }


    /// <summary>
    /// ��ȡ��������Ļ��˲��ԡ�
    /// </summary>
    /// <remarks>
    /// ��Ĭ������£�Windows ϵͳ���Դ������������壺
    /// <list type="bullet">
    /// <item>Windows 95�����塢���塢����_GB2312������_GB2312</item>
    /// <item>Windows XP������/�����塢���塢����_GB2312������_GB2312������-PUA</item>
    /// <item>Windows Vista������/�����塢���塢���塢���Ρ�΢���źڡ�SimSun-ExtB</item>
    /// <item>Windows 8������</item>
    /// </list>
    /// ��������Ϊ Microsoft Office �Դ���
    /// <list type="bullet">
    /// <item>Office�����顢��Բ���������塢����Ҧ�塢����ϸ�ڡ����Ŀ��塢�������塢�������Ρ����ķ��Ρ����Ĳ��ơ��������ꡢ�������顢�����п���������κ</item>
    /// </list>
    /// </remarks>
    /// <returns></returns>
    private static IDictionary<string, string> GetDefaultFallback() => new Dictionary<string, string>
    {
        // Windows �Դ����壺���ǽ� Office �������������ӳ�䵽����
        { "����", "΢���ź�" },
        { "���� Light", "΢���ź� Light" },
        { "����", "����" },
        { "������", "����" },
        { "����", "����" },
        { "΢���ź�", "����" },
        // Office �Դ����壺���ǽ���������ӳ�䵽 Office �Դ�������
        { "����", "����" },
        { "��Բ", "����" },
        { "��������", "����" },
        { "����Ҧ��", "����" },
        { "����ϸ��", "����" },
        { "���Ŀ���", "����" },
        { "��������", "����" },
        { "��������", "����" },
        { "���ķ���", "����" },
        { "���Ĳ���", "����" },
        { "��������", "����" },
        { "��������", "����" },
        { "�����п�", "����" },
        { "������κ", "����" },
        // �������壺���ǲ�Ҫ���������巶Χ�ڻ���ӳ�䣬����ӳ�����������
        { "ƻ��", "΢���ź�" },
    };

    private static IDictionary<string, string> GetFuzzyFallback() => new Dictionary<string, string>
    {
        // ��������
        { "��ͼС��", "���Ĳ���" },
        { "��������", "����" },
        { "��������", "��������" },
        { "������Բ", "����" },
        { "��Բ", "��Բ" }, // ������Բ
        { "��Բ", "��Բ" }, // ������Բ
        { "Բ��", "��Բ" }, // ����Բ��
        { "׭��", "����" }, // ����׭��
        { "�ֱ���", "����" }, // ��ʯ¼�ֱ���
        // ������
        { "���ֹ�������", "����" },
        { "��������", "����" }, // �ֶ���������
        { "����", "����" }, // �ֶ�����
        { "����", "����" }, // ��������
        { "����", "����" }, // ���ֹ�����������
        { "����", "����" }, // ������̱�����
        { "����", "����" }, // ��������
        { "����", "����" }, // ���Ǵ���
        { "����", "����" }, // ˼Դ����
        { "����", "����" }, // ��������
        { "����", "����" }, // ��ʯ¼���� Դ������
        { "����", "����" }, // ���ù������
        // �࿬��
        { "����", "����" },
        { "���ֹ�������", "����" },
        { "���ټ���", "����" },
        { "������צ", "����" },
        { "����өѩ", "����" },
        { "���ǲ����ο̱�", "����" },
        { "����", "����" },
        { "�忬", "����" },
        { "�㿬", "����" }, // �����ο̱��㿬
        { "����", "����" }, // ��������ʫ����
        { "����", "����" }, // ���Ǿ���
        { "����", "����" },
        // �����
        { "����", "΢���ź�" },
        { "��ͤ��", "΢���ź�" }, // ������ͤ�� ������ͤ��
        { "��ͤ�˺�", "΢���ź� Light" },
        { "��ͤ��ϸ��", "΢���ź� Light" },
        { "������", "����" }, // ������������
        { "��Բ", "����" }, // ���Ƿ�Բ
        { "�ú�", "΢���ź�" }, // ���ֹ����ú�
        { "����", "����" }, // ��������
        { "���", "����" }, // �������
        { "���", "����" }, // �����ſ�� վ����
        { "�߶˺�", "����" }, // վ��߶˺�
        { "���", "����" }, // ˼Դ���
        { "����", "����" }, // ������� ˼Դ����
        // ��Ҧ��
        { "�����Ƿ���", "����Ҧ��" },
        { "Ҧ��", "����Ҧ��" },
        // ����õ���ƥ�䣬������������������硰�ֶ��񿬡������������Ρ��������ֹ����úڡ���
        { "��", "����" },
        { "��", "����" },
        { "��", "����" }, // ���ֹ�������
        { "Բ", "��Բ" }, // ������ͤԲ���� ������Բ

        //ȡ��ע�����һ�У������޸Ļ���δ�ҵ�ʱ���Ĭ��ֵ��
        //{ "", "΢���ź�" },
    };
}