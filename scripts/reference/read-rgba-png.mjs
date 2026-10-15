import assert from 'node:assert/strict';
import {inflateSync} from 'node:zlib';

// Raw 8-bit RGB/RGBA comparison, without resampling or color substitutions.
// Other PNG formats fail explicitly rather than changing the comparison.
export function readRgbaPng(bytes){
 assert.equal(bytes.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
 let width,height,channels,ended=false;const parts=[];
 const crc32=data=>{let crc=0xffffffff;for(const byte of data){crc^=byte;for(let bit=0;bit<8;bit++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}return(crc^0xffffffff)>>>0;};
 for(let offset=8;offset<bytes.length;){
  assert.ok(offset+12<=bytes.length,'Truncated PNG chunk.');const length=bytes.readUInt32BE(offset),end=offset+8+length;assert.ok(end+4<=bytes.length);
  const type=bytes.toString('ascii',offset+4,offset+8),data=bytes.subarray(offset+8,end);assert.equal(crc32(bytes.subarray(offset+4,end)),bytes.readUInt32BE(end),'PNG CRC differs.');
  if(type==='IHDR'){
   assert.equal(width,undefined);assert.equal(length,13);width=data.readUInt32BE(0);height=data.readUInt32BE(4);assert.ok(width>0&&height>0);
   assert.equal(data[8],8,'Only 8-bit captures are supported.');assert.ok([2,6].includes(data[9]),'Only RGB/RGBA captures are supported.');channels=data[9]===2?3:4;
   assert.equal(data[10],0);assert.equal(data[11],0);assert.equal(data[12],0,'Interlaced captures require a separate explicit decoder.');
  }else if(type==='IDAT'){assert.notEqual(channels,undefined);parts.push(data);}
  else if(type==='IEND'){assert.equal(length,0);assert.equal(end+4,bytes.length);ended=true;break;}
  offset=end+4;
 }
 assert.equal(ended,true);assert.ok(parts.length>0);const stride=width*channels,encoded=inflateSync(Buffer.concat(parts));assert.equal(encoded.length,(stride+1)*height);
 const raw=Buffer.alloc(stride*height),rgba=Buffer.alloc(width*height*4);let cursor=0;
 const paeth=(a,b,c)=>{const estimate=a+b-c,da=Math.abs(estimate-a),db=Math.abs(estimate-b),dc=Math.abs(estimate-c);return da<=db&&da<=dc?a:db<=dc?b:c;};
 for(let y=0;y<height;y++){
  const filter=encoded[cursor++];assert.ok(filter<=4,'Unknown PNG filter.');
  for(let x=0;x<stride;x++){
   const index=y*stride+x,left=x>=channels?raw[index-channels]:0,above=y>0?raw[index-stride]:0,corner=y>0&&x>=channels?raw[index-stride-channels]:0;
   const predictor=filter===0?0:filter===1?left:filter===2?above:filter===3?Math.floor((left+above)/2):paeth(left,above,corner);raw[index]=(encoded[cursor++]+predictor)&255;
  }
 }
 for(let pixel=0;pixel<width*height;pixel++){const from=pixel*channels,to=pixel*4;rgba[to]=raw[from];rgba[to+1]=raw[from+1];rgba[to+2]=raw[from+2];rgba[to+3]=channels===4?raw[from+3]:255;}
 return{width,height,rgba};
}
