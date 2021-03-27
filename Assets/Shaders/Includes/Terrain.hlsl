float3 ColorForHeight(float Height, float BaseHeight, float Color, float Blend) {
  float drawStrength = saturate(sign(Height - BaseHeight));
  return (1 - drawStrength) + Color * drawStrength;
}

void ColorFromHeights_float(float Height, float4 Heights, float4 Colors, float4 Blends, out float3 Color) {
  float3 color = 0;

  color = color * ColorForHeight(Height, Heights.x, Colors.x, Blends.x);
  color = color * ColorForHeight(Height, Heights.y, Colors.y, Blends.y);
  color = color * ColorForHeight(Height, Heights.z, Colors.z, Blends.z);
  color = color * ColorForHeight(Height, Heights.w, Colors.w, Blends.w);

  Color = color;
}