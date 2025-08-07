// Copyright (c) 2020-2025 Beijing TOMs Software Technology Co., Ltd
using GWDataCenter;
using System.Collections.Concurrent;

namespace GWChangJing.STD
{
    /// <summary>
    /// �豸�����࣬�������豸�������ú�����ˢ��
    /// </summary>
    public class CEquip : CEquipBase, IDisposable
    {
        private readonly ConcurrentQueue<SetItem> _setItemQueue = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _refreshTask;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        private const int ThreadInterval = 100;
        private bool _disposed = false;

        public CEquip()
        {
            // ʹ��Task.Run����Thread���ṩ���õ��̹߳���
            _refreshTask = Task.Run(RefreshAsync, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// ������������
        /// </summary>
        /// <param name="item">Ҫ��ӵ�������</param>
        /// <exception cref="ArgumentNullException">��itemΪnullʱ�׳�</exception>
        /// <exception cref="ObjectDisposedException">���������ͷ�ʱ�׳�</exception>
        public void AddSetItem(SetItem item)
        {
            ThrowIfDisposed();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // ConcurrentQueue���̰߳�ȫ�ģ�����Ҫ�������
            // ʹ��Contains�����ܺܰ��󣬿����Ƿ������Ҫȥ��
            _setItemQueue.Enqueue(item);
        }

        /// <summary>
        /// �Ӷ����л�ȡ������
        /// </summary>
        /// <returns>������������Ϊ���򷵻�null</returns>
        /// <exception cref="ObjectDisposedException">���������ͷ�ʱ�׳�</exception>
        public SetItem GetSetItem()
        {
            ThrowIfDisposed();

            return _setItemQueue.TryDequeue(out SetItem item) ? item : null;
        }

        /// <summary>
        /// ��ȡ�豸����
        /// </summary>
        /// <param name="pEquip">�豸����</param>
        /// <returns>ͨ��״̬</returns>
        public override CommunicationState GetData(CEquipBase pEquip)
        {
            ThrowIfDisposed();
            return base.GetData(pEquip);
        }

        /// <summary>
        /// �����豸����
        /// </summary>
        /// <param name="mainInstruct">��ָ��</param>
        /// <param name="minorInstruct">��ָ��</param>
        /// <param name="value">����ֵ</param>
        /// <returns>�Ƿ�ɹ���ӵ�����</returns>
        /// <exception cref="ArgumentException">������Ϊ�ջ�nullʱ�׳�</exception>
        /// <exception cref="ObjectDisposedException">���������ͷ�ʱ�׳�</exception>
        public override bool SetParm(string mainInstruct, string minorInstruct, string value)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(mainInstruct))
                throw new ArgumentException("��ָ���Ϊ��", nameof(mainInstruct));

            if (string.IsNullOrWhiteSpace(minorInstruct))
                throw new ArgumentException("��ָ���Ϊ��", nameof(minorInstruct));

            try
            {
                var item = new SetItem(
                    equipitem.iEquipno,
                    mainInstruct,
                    minorInstruct,
                    value,
                    SetParmExecutor,
                    false
                );

                AddSetItem(item);
                return true;
            }
            catch (Exception)
            {
                // ��¼��־�����쳣
                return false;
            }
        }

        /// <summary>
        /// �첽ˢ�´�����
        /// </summary>
        private async Task RefreshAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var item = GetSetItem();

                    if (item != null)
                    {
                        // ʹ��Task.Run�첽��������������ѭ��
                        _ = Task.Run(() => ProcessSetItem(item), _cancellationTokenSource.Token);
                    }

                    // ʹ���첽�ӳ٣�����������������
                    await Task.Delay(ThreadInterval, _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // ����ȡ��������Ҫ����
            }
            catch (Exception ex)
            {
                // ��¼�쳣��־
                Console.WriteLine($"RefreshAsync�쳣: {ex.Message}");
            }
        }

        /// <summary>
        /// ����������
        /// </summary>
        /// <param name="item">Ҫ�����������</param>
        private void ProcessSetItem(SetItem item)
        {
            try
            {
                // ʹ���ź������Ʋ������������
                _semaphore.Wait(_cancellationTokenSource.Token);

                try
                {
                    //TODO
                    //GWDataCenter.DataCenter.DoCJ(item);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // ����ȡ��������Ҫ����
            }
            catch (Exception ex)
            {
                // ��¼�쳣��־
                Console.WriteLine($"ProcessSetItem�쳣: {ex.Message}");
            }
        }

        /// <summary>
        /// �������Ƿ����ͷ�
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CEquip));
        }

        /// <summary>
        /// �ͷ���Դ
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// �ͷ���Դ��ʵ��
        /// </summary>
        /// <param name="disposing">�Ƿ������ͷ��й���Դ</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cancellationTokenSource?.Cancel();

                try
                {
                    _refreshTask?.Wait(TimeSpan.FromSeconds(5));
                }
                catch (AggregateException)
                {
                    // ��ʱ�������쳣����������
                }

                _cancellationTokenSource?.Dispose();
                _semaphore?.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// ��������
        /// </summary>
        ~CEquip()
        {
            Dispose(false);
        }
    }
}