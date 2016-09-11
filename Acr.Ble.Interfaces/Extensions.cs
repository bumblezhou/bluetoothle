﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;


namespace Acr.Ble
{
    public static class Extensions
    {
        public static IObservable<IGattCharacteristic> WhenAnyCharacteristic(this IDevice device)
        {
            return device
                .WhenServiceDiscovered()
                .SelectMany(x => x.WhenCharacteristicDiscovered().Select(y => y));
        }


        public static IObservable<IGattDescriptor> WhenyAnyDescriptor(this IDevice device)
        {
            return device
                .WhenAnyCharacteristic()
                .SelectMany(x => x.WhenDescriptorDiscovered().Select(y => y));
        }


        public static async Task<IList<IGattCharacteristic>> GetAllCharacteristics(this IDevice device, int waitMillis = 2000)
        {
            var result = await device
                .WhenAnyCharacteristic()
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(waitMillis))
                .ToList();

            return result;
        }


        public static async Task<IList<IGattDescriptor>> GetAllDescriptors(this IDevice device, int waitMillis = 2000)
        {
            var result = await device
                .WhenyAnyDescriptor()
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(waitMillis))
                .ToList();

            return result;
        }


        public static IObservable<byte[]> ReadInterval(this IGattCharacteristic character, TimeSpan timeSpan)
        {
            return Observable.Create<byte[]>(ob =>
                Observable
                    .Interval(timeSpan)
                    .Subscribe(async _ =>
                    {
                        var read = await character.Read();
                        ob.OnNext(read);
                    })
            );
        }


        public static IObservable<IScanResult> ScanInterval(this IAdapter adapter, TimeSpan timeSpan)
        {
            return Observable.Create<IScanResult>(ob =>
            {
                var scanner = adapter
                    .Scan()
                    .Subscribe(ob.OnNext);

                var timer = Observable
                    .Interval(timeSpan)
                    .Subscribe(x =>
                    {
                        if (scanner == null)
                        {
                            scanner = adapter
                                .Scan()
                                .Subscribe(ob.OnNext);
                        }
                        else
                        {
                            scanner.Dispose();
                            scanner = null;
                        }
                    });

                return () =>
                {
                    timer.Dispose();
                    scanner?.Dispose();
                };
            });

        }


        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }


        public static bool CanWrite(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.WriteNoResponse) ||
                   ch.Properties.HasFlag(CharacteristicProperties.Write);
        }


        public static bool CanRead(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.Read);
        }


        public static bool CanNotify(this IGattCharacteristic ch)
        {
            return ch.Properties.HasFlag(CharacteristicProperties.Notify);
        }
    }
}
