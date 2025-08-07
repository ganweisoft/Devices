// Copyright (c) 2020-2025 Beijing TOMs Software Technology Co., Ltd
using GWDataCenter;
using GWDataCenter.Database;
using OpenGWDataCenter.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GWDataSimu.STD
{
    /// <summary>
    /// �豸ģ�����࣬����ģ���豸����ͨ��
    /// </summary>
    internal class CEquip : CEquipBase
    {
        #region ��������
        private const int DEFAULT_SLEEP_TIME = 500;
        private const int DEFAULT_COMM_CHECK_SLEEP = 100;
        private const int DEFAULT_FREQUENCY = 100000;
        private const string SETPARM_COUNT_INSTRUCTION = "SETPARMCOUNT";
        private const string SET_YC_YX_VALUE_INSTRUCTION = "SETYCYXVALUE";
        private const string ADD_EVENT_INSTRUCTION = "ADDEVENT";
        private const string SET_COMM_STATE_INSTRUCTION = "SETCOMMSTATE";
        private const string STRING_DATATYPE = "S";
        private const int DATETIME_PREFIX_LENGTH = 14;
        #endregion

        #region ˽���ֶ�
        private bool _firstEnter = true;
        private int _count;
        private readonly int _frequency = DEFAULT_FREQUENCY;
        private readonly Random _random = new Random(Guid.NewGuid().GetHashCode());
        private readonly object _commStateLock = new object();
        private bool _commState = true;
        private int _sleepTime = DEFAULT_SLEEP_TIME;
        private readonly Dictionary<int, int> _setParmCountDict = new Dictionary<int, int>();
        private int _icount;

        // Ԥ�����������ʽ���������
        private static readonly Regex NumberRegex = new Regex(@"^-?[1-9]\d*\.\d+$|^-?0\.\d+$|^-?[1-9]\d*$|^0$", RegexOptions.Compiled);
        private static readonly Regex StringRegex = new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);
        private static readonly Regex SequenceRegex = new Regex(@"^-?\d+\.?(\d+)?(,\-?\d+\.?(\d+)?){1,6}$", RegexOptions.Compiled);
        #endregion

        #region ��������
        /// <summary>
        /// ��ʼ���豸
        /// </summary>
        /// <param name="item">�豸��</param>
        /// <returns>��ʼ���Ƿ�ɹ�</returns>
        public override bool init(EquipItem item)
        {
            if (!CheckCommunicationState())
            {
                return false;
            }

            if (!base.init(item))
            {
                return false;
            }

            if (base.ResetFlag || _firstEnter)
            {
                InitializeSleepTime();
                InitializeSetParmCountDict(item);
                _firstEnter = false;
            }

            return true;
        }

        /// <summary>
        /// ��ȡ����
        /// </summary>
        /// <param name="pEquip">�豸����</param>
        /// <returns>ͨ��״̬</returns>
        public override CommunicationState GetData(CEquipBase pEquip)
        {
            base.Sleep(_sleepTime, true);

            if (!CheckCommunicationState())
            {
                return CommunicationState.fail; // ����0��ʾʧ��״̬
            }

            return base.GetData(pEquip);
        }

        /// <summary>
        /// ��ȡYC��ң�⣩����
        /// </summary>
        /// <param name="r">YC��������</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        public override bool GetYC(YcpTableRow r)
        {
            if (r == null) return false;

            // ��������ָ��
            if (IsSetParmCountInstruction(r))
            {
                return HandleSetParmCount(r);
            }

            // �����������
            var existingData = base.GetYCData(r);
            if (existingData != null && IsValidExistingData(existingData, r))
            {
                base.SetYCData(r, existingData);
                return true;
            }

            // ����������
            GenerateYCData(r);
            return true;
        }

        /// <summary>
        /// ��ȡYX��ң�ţ�����
        /// </summary>
        /// <param name="r">YX��������</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        public override bool GetYX(YxpTableRow r)
        {
            if (r == null) return false;

            var inversion = r.inversion;
            var yxNo = r.yx_no;
            var isHighLevel = r.level_r >= r.level_d;
            object value = 0;

            // ��datatypeΪnullʱ�Ĵ����߼�

            if (isHighLevel)
            {
                // �ߵ�ƽ״̬
                if (inversion)
                {
                    // �����߼����ߵ�ƽʱӦ��Ϊtrue
                    if (base.YXResults.ContainsKey(yxNo) && !Convert.ToBoolean(base.YXResults[yxNo]))
                    {
                        return true; // �����ǰֵΪfalse�����ֲ���
                    }
                    value = true;
                }
                else
                {
                    // �����߼����ߵ�ƽʱӦ��Ϊfalse
                    if (base.YXResults.ContainsKey(yxNo) && Convert.ToBoolean(base.YXResults[yxNo]))
                    {
                        return true; // �����ǰֵΪtrue�����ֲ���
                    }
                    value = false;
                }
            }
            else
            {
                // �͵�ƽ״̬
                if (inversion)
                {
                    // �����߼����͵�ƽʱӦ��Ϊfalse
                    if (base.YXResults.ContainsKey(yxNo) && Convert.ToBoolean(base.YXResults[yxNo]))
                    {
                        return true; // �����ǰֵΪtrue�����ֲ���
                    }
                    value = false;
                }
                else
                {
                    // �����߼����͵�ƽʱӦ��Ϊtrue
                    if (base.YXResults.ContainsKey(yxNo) && !Convert.ToBoolean(base.YXResults[yxNo]))
                    {
                        return true; // �����ǰֵΪfalse�����ֲ���
                    }
                    value = true;
                }
            }
            SetYXData(r, value);
            YXToPhysic(r);
            return true;
        }

        /// <summary>
        /// ���ò���
        /// </summary>
        /// <param name="mainInstruct">��ָ��</param>
        /// <param name="minorInstruct">��ָ��</param>
        /// <param name="value">ֵ</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        public override bool SetParm(string mainInstruct, string minorInstruct, string value)
        {
            if (string.IsNullOrEmpty(mainInstruct) || string.IsNullOrEmpty(minorInstruct))
            {
                return false;
            }

            try
            {
                return mainInstruct.ToUpper() switch
                {
                    SET_YC_YX_VALUE_INSTRUCTION => HandleSetYCYXValue(minorInstruct, value),
                    ADD_EVENT_INSTRUCTION => HandleAddEvent(minorInstruct),
                    SET_COMM_STATE_INSTRUCTION => HandleSetCommState(minorInstruct),
                    _ => false
                };
            }
            catch (Exception ex)
            {
                GWDataCenter.DataCenter.WriteLogFile($"SetParm error: {ex.Message}", LogType.Error);
                return false;
            }
        }

        /// <summary>
        /// ȷ�ϵ�����״̬
        /// </summary>
        /// <param name="sYcYxType">YC/YX����</param>
        /// <param name="iYcYxNo">YC/YX���</param>
        /// <returns>ȷ���Ƿ�ɹ�</returns>
        public override bool Confirm2NormalState(string sYcYxType, int iYcYxNo)
        {
            return true;
        }
        #endregion

        #region ˽�и�������
        /// <summary>
        /// ���ͨ��״̬
        /// </summary>
        /// <returns>ͨ���Ƿ�����</returns>
        private bool CheckCommunicationState()
        {
            lock (_commStateLock)
            {
                if (!_commState)
                {
                    base.Sleep(DEFAULT_COMM_CHECK_SLEEP, true);
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// ��ʼ������ʱ��
        /// </summary>
        private void InitializeSleepTime()
        {
            if (string.IsNullOrWhiteSpace(base.Equipitem.communication_param))
            {
                _sleepTime = DEFAULT_SLEEP_TIME;
            }
            else
            {
                int.TryParse(base.Equipitem.communication_param, out _sleepTime);
            }
        }

        /// <summary>
        /// ��ʼ�����ò��������ֵ�
        /// </summary>
        /// <param name="item">�豸��</param>
        private void InitializeSetParmCountDict(EquipItem item)
        {
            var setParmItems = StationItem.db_Setparm
                .Where(m => m.equip_no == item.iEquipno)
                .ToList();

            foreach (var setParmItem in setParmItems)
            {
                _setParmCountDict.TryAdd(setParmItem.set_no, 0);
            }
        }

        /// <summary>
        /// ����Ƿ�Ϊ���ò�������ָ��
        /// </summary>
        /// <param name="r">YC��������</param>
        /// <returns>�Ƿ�Ϊ���ò�������ָ��</returns>
        private bool IsSetParmCountInstruction(YcpTableRow r)
        {
            return r.main_instruction?.Trim().ToUpper() == SETPARM_COUNT_INSTRUCTION;
        }

        /// <summary>
        /// �������ò�������
        /// </summary>
        /// <param name="r">YC��������</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleSetParmCount(YcpTableRow r)
        {
            try
            {
                if (int.TryParse(r.minor_instruction?.Trim(), out int key) &&
                    _setParmCountDict.ContainsKey(key))
                {
                    base.SetYCData(r, _setParmCountDict[key]);
                }
                return true;
            }
            catch (Exception ex)
            {
                GWDataCenter.DataCenter.WriteLogFile($"HandleSetParmCount error: {ex.Message}", LogType.Error);
                return true; // ��ʹ����Ҳ����true������ԭ���߼�
            }
        }

        /// <summary>
        /// ������������Ƿ���Ч
        /// </summary>
        /// <param name="existingData">��������</param>
        /// <param name="r">YC��������</param>
        /// <returns>�����Ƿ���Ч</returns>
        private bool IsValidExistingData(object existingData, YcpTableRow r)
        {
            try
            {
                if (double.TryParse(existingData.ToString(), out double value))
                {
                    return (value <= r.val_min || value >= r.val_max);
                }

                // �����ַ�����������
                if (existingData is string)
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ����YC����
        /// </summary>
        /// <param name="r">YC��������</param>
        private void GenerateYCData(YcpTableRow r)
        {
            var dataType = string.Empty;

            if (string.IsNullOrEmpty(dataType))
            {
                base.SetYCData(r, CreateYCValue(r));
            }
            else if (dataType == STRING_DATATYPE)
            {
                base.SetYCData(r, CreateYCValue(r).ToString());
            }
            else if (dataType.StartsWith("SEQ") && int.TryParse(dataType.Substring(3), out int seqCount))
            {
                GenerateSequenceData(r, seqCount);
            }
        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="r">YC��������</param>
        /// <param name="count">��������</param>
        private void GenerateSequenceData(YcpTableRow r, int count)
        {
            var values = new double[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = CreateYCValue(r);
            }

            var tuple = count switch
            {
                2 => (object)(values[0], values[1]),
                3 => (values[0], values[1], values[2]),
                4 => (values[0], values[1], values[2], values[3]),
                5 => (values[0], values[1], values[2], values[3], values[4]),
                6 => (values[0], values[1], values[2], values[3], values[4], values[5]),
                7 => (values[0], values[1], values[2], values[3], values[4], values[5], values[6]),
                _ => (object)values[0]
            };

            base.SetYCData(r, tuple);
        }

        /// <summary>
        /// ����YCֵ
        /// </summary>
        /// <param name="r">YC��������</param>
        /// <returns>���ɵ�YCֵ</returns>
        private double CreateYCValue(YcpTableRow r)
        {
            var valMin = r.val_min;
            var valMax = r.val_max;

            // ��������������־λ�����ɳ�����Χ��ֵ
            if ((r.val_trait & 1) > 0)
            {
                return _random.Next((int)(valMax * 1.1), (int)(valMax * 1.2));
            }

            // ������Χ������ֵ
            if (valMin + 1.0 <= valMax - 1.0)
            {
                return _random.Next((int)valMin + 1, (int)valMax - 1);
            }

            // ��Χ��Сʱ�Ĵ���
            if (valMax - valMin <= 1.0)
            {
                return _random.Next((int)(valMin * 100.0 + 1.0), (int)(valMax * 100.0 - 1.0)) / 100.0;
            }

            return _random.Next((int)valMin, (int)valMax);
        }

        /// <summary>
        /// ��������YC/YXֵ
        /// </summary>
        /// <param name="minorInstruct">��ָ��</param>
        /// <param name="value">ֵ</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleSetYCYXValue(string minorInstruct, string value)
        {
            if (minorInstruct.Length <= 2 || string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (!int.TryParse(minorInstruct.Substring(2), out int number) || number <= 0)
            {
                return false;
            }

            var equipItem = GWDataCenter.DataCenter.GetEquipItem(base.m_equip_no);
            if (equipItem == null)
            {
                return false;
            }

            var firstChar = char.ToUpper(minorInstruct[0]);

            return firstChar switch
            {
                'C' => HandleSetYCValue(equipItem, number, value),
                'X' => HandleSetYXValue(number, value),
                _ => false
            };
        }

        /// <summary>
        /// ��������YCֵ
        /// </summary>
        /// <param name="equipItem">�豸��</param>
        /// <param name="number">���</param>
        /// <param name="value">ֵ</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleSetYCValue(EquipItem equipItem, int number, string value)
        {
            lock (base.YCResults)
            {
                var dateTimeIndex = value.IndexOf(':');

                if (dateTimeIndex == DATETIME_PREFIX_LENGTH)
                {
                    var parsedValue = ParseDateTimeValue(value, dateTimeIndex);
                    equipItem.YCItemDict[number].YCValue = parsedValue;
                }
                else
                {
                    try
                    {
                        var doubleValue = Convert.ToDouble(value);
                        equipItem.YCItemDict[number].YCValue = doubleValue;
                        base.YCResults[number] = doubleValue;
                    }
                    catch
                    {
                        equipItem.YCItemDict[number].YCValue = value;
                        base.YCResults[number] = value;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// ��������YXֵ
        /// </summary>
        /// <param name="number">���</param>
        /// <param name="value">ֵ</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleSetYXValue(int number, string value)
        {
            lock (base.YXResults)
            {
                base.YXResults[number] = Convert.ToInt32(value) > 0;
            }

            return true;
        }

        /// <summary>
        /// ��������ʱ��ֵ
        /// </summary>
        /// <param name="value">ֵ�ַ���</param>
        /// <param name="dateTimeIndex">����ʱ������</param>
        /// <returns>�������Ԫ��</returns>
        private (DateTime, object) ParseDateTimeValue(string value, int dateTimeIndex)
        {
            var dateTimeStr = value.Substring(0, dateTimeIndex);
            var dataStr = value.Substring(dateTimeIndex + 1);

            var dateTime = DateTime.ParseExact(dateTimeStr, "yyyyMMddHHmmss", null);

            // ���Խ�����ͬ���͵�����
            if (NumberRegex.IsMatch(dataStr))
            {
                return (dateTime, Convert.ToDouble(dataStr));
            }

            if (StringRegex.IsMatch(dataStr))
            {
                return (dateTime, dataStr);
            }

            if (SequenceRegex.IsMatch(dataStr))
            {
                var sequenceData = ParseSequenceData(dataStr);
                return (dateTime, sequenceData);
            }

            return (dateTime, dataStr);
        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="dataStr">�����ַ���</param>
        /// <returns>����������ж���</returns>
        private object ParseSequenceData(string dataStr)
        {
            var parts = dataStr.Split(',');
            var values = parts.Select(double.Parse).ToArray();

            return values.Length switch
            {
                2 => (values[0], values[1]),
                3 => (values[0], values[1], values[2]),
                4 => (values[0], values[1], values[2], values[3]),
                5 => (values[0], values[1], values[2], values[3], values[4]),
                6 => (values[0], values[1], values[2], values[3], values[4], values[5]),
                7 => (values[0], values[1], values[2], values[3], values[4], values[5], values[6]),
                _ => (object)values[0]
            };
        }

        /// <summary>
        /// ��������¼�
        /// </summary>
        /// <param name="minorInstruct">��ָ��</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleAddEvent(string minorInstruct)
        {
            var equipEvent = new EquipEvent(minorInstruct, MessageLevel.Info, DateTime.Now);
            base.EquipEventList.Add(equipEvent);
            return true;
        }

        /// <summary>
        /// ��������ͨ��״̬
        /// </summary>
        /// <param name="minorInstruct">��ָ��</param>
        /// <returns>�����Ƿ�ɹ�</returns>
        private bool HandleSetCommState(string minorInstruct)
        {
            if (minorInstruct.Trim() == "0")
            {
                lock (_commStateLock)
                {
                    _commState = false;
                }
            }

            return true;
        }

        /// <summary>
        /// ��������Ƿ���Ч
        /// </summary>
        /// <param name="type">����</param>
        /// <returns>�����Ƿ���Ч</returns>
        private bool CheckType(Type type)
        {
            var genericArguments = type.GetGenericArguments();
            return genericArguments.Length == 2 && genericArguments[0] == typeof(DateTime);
        }
        #endregion
    }
}