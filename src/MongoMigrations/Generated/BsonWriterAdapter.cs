using System.CodeDom.Compiler;
using MongoDB.Bson.IO;
using System;
using MongoDB.Bson;

namespace MongoDB.Bson.IO.Generator
{
    [GeneratedCode("AutoAdapter", "0.1")]
    public partial class BsonWriterAdapter : IBsonWriter
    {
		public IBsonWriter inner { get; }
		public BsonWriterAdapter(IBsonWriter inner) => this.inner = inner;

		public virtual long Position {
			get => this.inner.Position;
		}
		public virtual int SerializationDepth {
			get => this.inner.SerializationDepth;
		}
		public virtual BsonWriterSettings Settings {
			get => this.inner.Settings;
		}
		public virtual BsonWriterState State {
			get => this.inner.State;
		}

		public virtual void Close() => this.inner.Close();
		public virtual void Flush() => this.inner.Flush();
		public virtual void PopElementNameValidator() => this.inner.PopElementNameValidator();
		public virtual void PopSettings() => this.inner.PopSettings();
		public virtual void PushElementNameValidator(IElementNameValidator validator) => this.inner.PushElementNameValidator(validator);
		public virtual void PushSettings(Action<BsonWriterSettings> configurator) => this.inner.PushSettings(configurator);
		public virtual void WriteBinaryData(BsonBinaryData binaryData) => this.inner.WriteBinaryData(binaryData);
		public virtual void WriteBoolean(bool value) => this.inner.WriteBoolean(value);
		public virtual void WriteBytes(byte[] bytes) => this.inner.WriteBytes(bytes);
		public virtual void WriteDateTime(long value) => this.inner.WriteDateTime(value);
		public virtual void WriteDecimal128(Decimal128 value) => this.inner.WriteDecimal128(value);
		public virtual void WriteDouble(double value) => this.inner.WriteDouble(value);
		public virtual void WriteEndArray() => this.inner.WriteEndArray();
		public virtual void WriteEndDocument() => this.inner.WriteEndDocument();
		public virtual void WriteInt32(int value) => this.inner.WriteInt32(value);
		public virtual void WriteInt64(long value) => this.inner.WriteInt64(value);
		public virtual void WriteJavaScript(string code) => this.inner.WriteJavaScript(code);
		public virtual void WriteJavaScriptWithScope(string code) => this.inner.WriteJavaScriptWithScope(code);
		public virtual void WriteMaxKey() => this.inner.WriteMaxKey();
		public virtual void WriteMinKey() => this.inner.WriteMinKey();
		public virtual void WriteName(string name) => this.inner.WriteName(name);
		public virtual void WriteNull() => this.inner.WriteNull();
		public virtual void WriteObjectId(ObjectId objectId) => this.inner.WriteObjectId(objectId);
		public virtual void WriteRawBsonArray(IByteBuffer slice) => this.inner.WriteRawBsonArray(slice);
		public virtual void WriteRawBsonDocument(IByteBuffer slice) => this.inner.WriteRawBsonDocument(slice);
		public virtual void WriteRegularExpression(BsonRegularExpression regex) => this.inner.WriteRegularExpression(regex);
		public virtual void WriteStartArray() => this.inner.WriteStartArray();
		public virtual void WriteStartDocument() => this.inner.WriteStartDocument();
		public virtual void WriteString(string value) => this.inner.WriteString(value);
		public virtual void WriteSymbol(string value) => this.inner.WriteSymbol(value);
		public virtual void WriteTimestamp(long value) => this.inner.WriteTimestamp(value);
		public virtual void WriteUndefined() => this.inner.WriteUndefined();
		public virtual void Dispose() => this.inner.Dispose();

    }
}
