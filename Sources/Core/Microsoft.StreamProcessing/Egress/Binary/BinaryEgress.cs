﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System.IO;
using Microsoft.StreamProcessing.Serializer;

namespace Microsoft.StreamProcessing
{
    public static partial class Streamable
    {
        /// <summary>
        /// Serialize streamable into a binary stream
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="input"></param>
        /// <param name="binaryStream"></param>
        /// <param name="writePropertiesToStream"></param>
        public static void ToBinaryStream<TKey, TPayload>(this IStreamable<TKey, TPayload> input, Stream binaryStream, bool writePropertiesToStream = false)
        {
            if (writePropertiesToStream)
            {
                var propSer = StreamableSerializer.Create<SerializedProperties>();
                propSer.Serialize(binaryStream, SerializedProperties.FromStreamProperties(input.Properties));
            }
            input.ToStreamMessageObservable()
                .Subscribe(new BinaryStreamObserver<TKey, TPayload>(input.Properties, binaryStream));
        }

        /// <summary>
        /// Serialize streamable into a binary file
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="input"></param>
        /// <param name="fileName"></param>
        public static void ToBinaryStream<TKey, TPayload>(this IStreamable<TKey, TPayload> input, string fileName)
        {
            using (var buffer = new FileStream(fileName, FileMode.Create))
            {
                input.ToBinaryStream(buffer);
            }
        }

        /// <summary>
        /// Serialize streamable into a binary stream
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="container">The query container to which an egress point is being added.</param>
        /// <param name="identifier">A string that can uniquely identify the point of egress in the query.</param>
        /// <param name="input"></param>
        /// <param name="binaryStream"></param>
        /// <param name="writePropertiesToStream"></param>
        public static void RegisterBinaryOutput<TKey, TPayload>(this QueryContainer container, IStreamable<TKey, TPayload> input, Stream binaryStream, bool writePropertiesToStream = false, string identifier = null)
        {
            if (writePropertiesToStream)
            {
                var propSer = StreamableSerializer.Create<SerializedProperties>();
                propSer.Serialize(binaryStream, SerializedProperties.FromStreamProperties(input.Properties));
            }
            container.RegisterOutputAsStreamMessages(input, identifier)
                .Subscribe(new BinaryStreamObserver<TKey, TPayload>(input.Properties, binaryStream));
        }

        /// <summary>
        /// Serialize streamable into a binary file
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="container">The query container to which an egress point is being added.</param>
        /// <param name="identifier">A string that can uniquely identify the point of egress in the query.</param>
        /// <param name="input"></param>
        /// <param name="fileName"></param>
        public static void RegisterBinaryOutput<TKey, TPayload>(this QueryContainer container, IStreamable<TKey, TPayload> input, string fileName, string identifier = null)
        {
            using (var buffer = new FileStream(fileName, FileMode.Create))
            {
                container.RegisterBinaryOutput(input, buffer, false, identifier);
            }
        }

        internal static void ToTextStream<TKey, TPayload>(this IStreamable<TKey, TPayload> input, Stream textStream)
        {
            using (var sw = new StreamWriter(textStream))
            {
                input
                    .ToStreamMessageObservable()
                    .SynchronousForEach(message =>
                    {
                        var bv = message.bitvector.col;
                        for (int i = 0; i < message.Count; i++)
                        {
                            if ((bv[i >> 6] & (1L << (i & 0x3f))) == 0)
                            {
                                sw.WriteLine("{0}\t{1}\t{2}\t{3}", message.vsync.col[i], message.vother.col[i], message.key.col[i], message[i]);
                            }
                        }
                        message.Free();
                    });
            }
        }

        internal static void ToTextFile<TKey, TPayload>(this IStreamable<TKey, TPayload> input, string filename)
        {
            using (var buffer = new FileStream(filename, FileMode.Create))
            {
                input.ToTextStream(buffer);
            }
        }
    }
}
